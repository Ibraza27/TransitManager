using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.Core.Messages;
using TransitManager.Core.Exceptions;

namespace TransitManager.WPF.ViewModels
{
    public class ClientDetailViewModel : BaseViewModel
    {
        private readonly IClientService _clientService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
		private readonly IMessenger _messenger;
		public Action? CloseAction { get; set; }
		private bool _isModal = false;
		public decimal ImpayesColis => Client?.Colis.Sum(c => c.RestantAPayer) ?? 0;
		public decimal ImpayesVehicules => Client?.Vehicules.Sum(v => v.RestantAPayer) ?? 0;
		
		public void SetModalMode()
		{
			_isModal = true;
		}

        private Client? _client;
        public Client? Client
        {
            get => _client;
            set => SetProperty(ref _client, value);
        }

        private bool _isNewClient;

        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public ClientDetailViewModel(IClientService clientService, INavigationService navigationService, IDialogService dialogService, IMessenger messenger)
        {
            _clientService = clientService;
            _navigationService = navigationService;
            _dialogService = dialogService;
			_messenger = messenger;

            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private bool CanSave()
        {
            return Client != null &&
                   !string.IsNullOrWhiteSpace(Client.Nom) &&
                   !string.IsNullOrWhiteSpace(Client.Prenom) &&
                   !string.IsNullOrWhiteSpace(Client.TelephonePrincipal) &&
                   !IsBusy;
        }

		private async Task SaveAsync()
		{
			if (!CanSave() || Client == null) return;

			await ExecuteBusyActionAsync(async () =>
			{
				try
				{
					if (_isNewClient)
					{
						await _clientService.CreateAsync(Client);
					}
					else
					{
						await _clientService.UpdateAsync(Client);
					}
					
					await _dialogService.ShowInformationAsync("Succès", "Le client a été enregistré.");
					_messenger.Send(new ClientUpdatedMessage(true));

					if (_isModal)
					{
						CloseAction?.Invoke();
					}
					else
					{
						_navigationService.GoBack();
					}
				}
				// ======================= DÉBUT DE LA MODIFICATION =======================
				catch (ConcurrencyException cex)
				{
					var refresh = await _dialogService.ShowConfirmationAsync(
						"Conflit de Données",
						$"{cex.Message}\n\nVoulez-vous rafraîchir les données pour voir les dernières modifications ? (Vos changements actuels seront perdus)");

					if (refresh && Client != null)
					{
						// Recharge les données depuis la BDD pour écraser les changements locaux.
						await InitializeAsync(Client.Id);
					}
				}
				// ======================== FIN DE LA MODIFICATION ========================
				catch (Exception ex)
				{
					await _dialogService.ShowErrorAsync("Erreur", $"Une erreur est survenue : {ex.Message}");
				}
			});
		}

		private void Cancel()
		{
			if (_isModal)
			{
				CloseAction?.Invoke();
			}
			else
			{
				_navigationService.GoBack();
			}
		}

        public async Task InitializeAsync(string newMarker)
        {
            if (newMarker == "new")
            {
                Title = "Nouveau Client";
                Client = new Client();
                _isNewClient = true;
                Client.PropertyChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
            }
        }

		public async Task InitializeAsync(Guid clientId)
		{
			// On garde ExecuteBusyActionAsync pour l'indicateur de chargement
			await ExecuteBusyActionAsync(async () =>
			{
				Client = await _clientService.GetByIdAsync(clientId); 
				if (Client != null)
				{
					Title = $"Modifier - {Client.NomComplet}";
					_isNewClient = false;
					
					// On notifie l'interface que les propriétés calculées sont à jour
					OnPropertyChanged(nameof(ImpayesColis));
					OnPropertyChanged(nameof(ImpayesVehicules));

					Client.PropertyChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
				}
			});
			// On notifie une nouvelle fois après la fin de IsBusy pour être sûr
			OnPropertyChanged(nameof(ImpayesColis));
			OnPropertyChanged(nameof(ImpayesVehicules));
		}
    }
}