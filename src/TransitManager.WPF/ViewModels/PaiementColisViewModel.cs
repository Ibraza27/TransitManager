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

namespace TransitManager.WPF.ViewModels
{
    public class PaiementColisViewModel : BaseViewModel
    {
        private readonly IPaiementService _paiementService;
        private readonly IDialogService _dialogService;
        private Guid _clientId;
        private Guid _colisId;

        private Paiement _newPaiement = new();
        public ObservableCollection<Paiement> Items { get; } = new();

        public Paiement NewPaiement { get => _newPaiement; set => SetProperty(ref _newPaiement, value); }

        private decimal _prixTotalColis;
        public decimal PrixTotalColis { get => _prixTotalColis; set => SetProperty(ref _prixTotalColis, value); }
        
        public int TotalPaiements => Items.Count;
        public decimal TotalValeur => Items.Sum(i => i.Montant);
        public decimal RestantAPayer => PrixTotalColis - TotalValeur;

        public IAsyncRelayCommand AddItemCommand { get; }
        public IAsyncRelayCommand<Paiement> RemoveItemCommand { get; }
        public IAsyncRelayCommand<Paiement> UpdateItemCommand { get; }

        public PaiementColisViewModel(IPaiementService paiementService, IDialogService dialogService)
        {
            _paiementService = paiementService;
            _dialogService = dialogService;

            AddItemCommand = new AsyncRelayCommand(AddItem, CanAddItem);
            RemoveItemCommand = new AsyncRelayCommand<Paiement>(RemoveItem);
            UpdateItemCommand = new AsyncRelayCommand<Paiement>(UpdateItem);

            NewPaiement.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
            Items.CollectionChanged += (s, e) => UpdateTotals();
        }

        public async Task InitializeAsync(Colis colis)
        {
            _clientId = colis.ClientId;
            _colisId = colis.Id;
            PrixTotalColis = colis.PrixTotal;
            
            var existingPaiements = await _paiementService.GetByColisAsync(_colisId);
            Items.Clear();
            foreach (var p in existingPaiements)
            {
                p.PropertyChanged += OnItemPropertyChanged;
                Items.Add(p);
            }
            ResetNewPaiement();
            UpdateTotals();
        }
        
        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Paiement editedPaiement)
            {
                UpdateItemCommand.Execute(editedPaiement);
            }
        }

        private void UpdateTotals()
        {
            OnPropertyChanged(nameof(TotalPaiements));
            OnPropertyChanged(nameof(TotalValeur));
            OnPropertyChanged(nameof(RestantAPayer));
        }

        private bool CanAddItem() => NewPaiement.Montant > 0;

        private async Task AddItem()
        {
            var paiementToAdd = NewPaiement;
            paiementToAdd.ClientId = _clientId;
            paiementToAdd.ColisId = _colisId;
            
            var createdPaiement = await _paiementService.CreateAsync(paiementToAdd);
            createdPaiement.PropertyChanged += OnItemPropertyChanged;
            Items.Add(createdPaiement);
            
            ResetNewPaiement();
        }

        private async Task RemoveItem(Paiement? item)
        {
            if (item == null) return;

            var confirm = await _dialogService.ShowConfirmationAsync("Confirmation", "Voulez-vous vraiment supprimer ce paiement ?");
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
            NewPaiement = new Paiement { ClientId = _clientId, ColisId = _colisId };
            NewPaiement.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
        }
    }
}