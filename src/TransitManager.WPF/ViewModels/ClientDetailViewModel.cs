using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;

namespace TransitManager.WPF.ViewModels
{
    public class ClientDetailViewModel : BaseViewModel
    {
        private readonly IClientService _clientService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;

        private Client? _client;
        public Client? Client
        {
            get => _client;
            set => SetProperty(ref _client, value);
        }

        public IAsyncRelayCommand SaveCommand { get; }
        public IRelayCommand CancelCommand { get; }

        public ClientDetailViewModel(IClientService clientService, INavigationService navigationService, IDialogService dialogService)
        {
            _clientService = clientService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            Title = "Détail du Client";

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
			if (!CanSave()) return;

			await ExecuteBusyActionAsync(async () =>
			{
				try
				{
					bool isNewClient = Client!.CreePar == null; 

					if (isNewClient)
					{
						await _clientService.CreateAsync(Client);
					}
					else
					{
						await _clientService.UpdateAsync(Client);
					}

					await _dialogService.ShowInformationAsync("Succès", "Le client a été enregistré avec succès.");
					
					// On re-navigue vers la liste des clients pour forcer un rechargement propre.
					_navigationService.NavigateTo("Clients"); 
				}
				catch (Exception ex)
				{
					// Affichez l'exception interne pour un meilleur débogage !
					var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
					await _dialogService.ShowErrorAsync("Erreur", $"Une erreur est survenue lors de l'enregistrement : {errorMessage}");
				}
			});
		}

		private void Cancel()
		{
			// L'annulation peut simplement revenir en arrière sans recharger.
			_navigationService.GoBack();
		}

        public Task InitializeAsync(string newClientMarker)
        {
            if (newClientMarker == "new")
            {
                Title = "Nouveau Client";
                Client = new Client();
                Client.PropertyChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
            }
            return Task.CompletedTask;
        }

        public async Task InitializeAsync(Guid clientId)
        {
            Title = "Modifier le Client";
            Client = await _clientService.GetByIdAsync(clientId);
            if (Client != null)
            {
                Client.PropertyChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }
}