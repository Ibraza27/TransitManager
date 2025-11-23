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
    public class PaiementColisViewModel : BaseViewModel
    {
        private readonly IPaiementService _paiementService;
        private readonly IDialogService _dialogService;
		private readonly IMessenger _messenger;
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

        public PaiementColisViewModel(IPaiementService paiementService, IDialogService dialogService, IMessenger messenger) 
        {
            _paiementService = paiementService;
            _dialogService = dialogService;
			_messenger = messenger; 

            AddItemCommand = new AsyncRelayCommand(AddItem, CanAddItem);
            RemoveItemCommand = new AsyncRelayCommand<Paiement>(RemoveItem);
            UpdateItemCommand = new AsyncRelayCommand<Paiement>(UpdateItem);

            NewPaiement.PropertyChanged += (s, e) => AddItemCommand.NotifyCanExecuteChanged();
            Items.CollectionChanged += (s, e) => UpdateTotals();
        }

		private bool _isInitializing = false; // AJOUT

		public async Task InitializeAsync(Guid colisId, Guid clientId, decimal prixTotalColis)
		{
			_isInitializing = true; // BLOQUER

			_colisId = colisId;
			_clientId = clientId;
			PrixTotalColis = prixTotalColis;
			
			var existingPaiements = await _paiementService.GetByColisAsync(_colisId);
			Items.Clear(); // Ne déclenche plus l'envoi
			
			foreach (var p in existingPaiements)
			{
				p.PropertyChanged += OnItemPropertyChanged;
				Items.Add(p);
			}
			ResetNewPaiement();
			
			_isInitializing = false; // DÉBLOQUER
			UpdateTotals();
		}

		private void UpdateTotals()
		{
			if (_isInitializing) return; // VÉRIFICATION

			OnPropertyChanged(nameof(TotalPaiements));
			OnPropertyChanged(nameof(TotalValeur));
			OnPropertyChanged(nameof(RestantAPayer));
			_messenger.Send(new EntityTotalPaidUpdatedMessage(_colisId, TotalValeur));
		}
        
        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Paiement editedPaiement)
            {
                UpdateItemCommand.Execute(editedPaiement);
            }
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