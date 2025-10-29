using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.Core.Messages;
using TransitManager.Core.Exceptions;
using System.Linq; // <-- ASSUREZ-VOUS QUE CE USING EST PRÉSENT

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

        // --- DÉBUT DE LA MODIFICATION 1 ---
		public decimal ImpayesColis => Client?.Colis?.Where(c => c.Actif).Sum(c => c.RestantAPayer) ?? 0;
		public decimal ImpayesVehicules => Client?.Vehicules?.Where(v => v.Actif).Sum(v => v.RestantAPayer) ?? 0;
		// --- FIN DE LA MODIFICATION 1 ---

		public void SetModalMode()
		{
			_isModal = true;
		}

        private Client? _client;
        public Client? Client
        {
            get => _client;
            // --- DÉBUT DE LA MODIFICATION 2 ---
            set 
            {
                if (SetProperty(ref _client, value))
                {
                    // Notifier explicitement que les propriétés calculées doivent être mises à jour
                    OnPropertyChanged(nameof(ImpayesColis));
                    OnPropertyChanged(nameof(ImpayesVehicules));
                }
            }
            // --- FIN DE LA MODIFICATION 2 ---
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
				catch (ConcurrencyException cex)
				{
					var refresh = await _dialogService.ShowConfirmationAsync(
						"Conflit de Données",
						$"{cex.Message}\n\nVoulez-vous rafraîchir les données pour voir les dernières modifications ? (Vos changements actuels seront perdus)");

					if (refresh && Client != null)
					{
						await InitializeAsync(Client.Id);
					}
				}
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
			await ExecuteBusyActionAsync(async () =>
			{
				Client = await _clientService.GetByIdAsync(clientId); 
				if (Client != null)
				{
					Title = $"Modifier - {Client.NomComplet}";
					_isNewClient = false;
					
					// La notification est maintenant gérée par le setter de la propriété Client
					Client.PropertyChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
				}
			});
		}
    }
}