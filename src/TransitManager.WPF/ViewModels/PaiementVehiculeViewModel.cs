using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.Core.Messages;

namespace TransitManager.WPF.ViewModels
{
    public class PaiementVehiculeViewModel : BaseViewModel
    {
        private readonly IPaiementService _paiementService;
        private readonly IDialogService _dialogService;
        private readonly IMessenger _messenger;
        private Guid _clientId;
        private Guid _vehiculeId;

        // --- AJOUT : Drapeau d'initialisation ---
        private bool _isInitializing = false;

        private Paiement _newPaiement = new();
        public ObservableCollection<Paiement> Items { get; } = new();

        public Paiement NewPaiement { get => _newPaiement; set => SetProperty(ref _newPaiement, value); }

        private decimal _prixTotalVehicule;
        public decimal PrixTotalVehicule { get => _prixTotalVehicule; set => SetProperty(ref _prixTotalVehicule, value); }

        public int TotalPaiements => Items.Count;
        public decimal TotalValeur => Items.Sum(i => i.Montant);
        public decimal RestantAPayer => PrixTotalVehicule - TotalValeur;

        public IAsyncRelayCommand AddItemCommand { get; }
        public IAsyncRelayCommand<Paiement> RemoveItemCommand { get; }
        public IAsyncRelayCommand<Paiement> UpdateItemCommand { get; }

        public PaiementVehiculeViewModel(IPaiementService paiementService, IDialogService dialogService, IMessenger messenger)
        {
            _paiementService = paiementService;
            _dialogService = dialogService;
            _messenger = messenger;
            Title = "Détails des Paiements du Véhicule";

            AddItemCommand = new AsyncRelayCommand(AddItem, CanAddItem);
            RemoveItemCommand = new AsyncRelayCommand<Paiement>(RemoveItem);
            UpdateItemCommand = new AsyncRelayCommand<Paiement>(UpdateItem);

            NewPaiement.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
            Items.CollectionChanged += (s, e) => UpdateTotals();
        }

        public async Task InitializeAsync(Guid vehiculeId, Guid clientId, decimal prixTotalVehicule)
        {
            // --- DÉBUT MODIFICATION ---
            _isInitializing = true; // Bloque les notifications

            _vehiculeId = vehiculeId;
            _clientId = clientId;
            PrixTotalVehicule = prixTotalVehicule;
            
            var existingPaiements = await _paiementService.GetByVehiculeAsync(_vehiculeId);
            
            Items.Clear(); // Ne déclenchera pas l'envoi de message "0" grâce au drapeau
            
            foreach (var p in existingPaiements)
            {
                p.PropertyChanged += OnItemPropertyChanged;
                Items.Add(p);
            }
            
            ResetNewPaiement();

            _isInitializing = false; // Débloque les notifications
            UpdateTotals(); // Force une mise à jour finale avec les vraies valeurs
            // --- FIN MODIFICATION ---
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Paiement editedPaiement) { UpdateItemCommand.Execute(editedPaiement); }
        }

        private void UpdateTotals()
        {
            // --- AJOUT : Garde de sécurité ---
            if (_isInitializing) return; 

            OnPropertyChanged(nameof(TotalPaiements));
            OnPropertyChanged(nameof(TotalValeur));
            OnPropertyChanged(nameof(RestantAPayer));
            _messenger.Send(new EntityTotalPaidUpdatedMessage(_vehiculeId, TotalValeur));
        }

        private bool CanAddItem() => NewPaiement.Montant > 0;

        private async Task AddItem()
        {
            var paiementToAdd = NewPaiement;
            paiementToAdd.ClientId = _clientId;
            paiementToAdd.VehiculeId = _vehiculeId;
            
            var createdPaiement = await _paiementService.CreateAsync(paiementToAdd);
            createdPaiement.PropertyChanged += OnItemPropertyChanged;
            Items.Add(createdPaiement);
            
            ResetNewPaiement();
        }

        private async Task RemoveItem(Paiement? item)
        {
            if (item == null) return;
            var confirm = await _dialogService.ShowConfirmationAsync("Confirmation", "Supprimer ce paiement ?");
            if (confirm)
            {
                await _paiementService.DeleteAsync(item.Id);
                item.PropertyChanged -= OnItemPropertyChanged;
                Items.Remove(item);
            }
        }
        
        private async Task UpdateItem(Paiement? item)
        {
            if (item == null) return;
            await _paiementService.UpdateAsync(item);
            UpdateTotals();
        }

        private void ResetNewPaiement()
        {
            NewPaiement = new Paiement { ClientId = _clientId, VehiculeId = _vehiculeId };
            NewPaiement.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
        }
    }
}