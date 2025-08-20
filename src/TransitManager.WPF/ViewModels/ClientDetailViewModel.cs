using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.WPF.Messages;

namespace TransitManager.WPF.ViewModels
{
    public class ClientDetailViewModel : BaseViewModel
    {
        private readonly IClientService _clientService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
		private readonly IMessenger _messenger;

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
                    
                    // --- ENVOI DU MESSAGE ---
                    _messenger.Send(new ClientUpdatedMessage(true));

                    _navigationService.GoBack();
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorAsync("Erreur", $"Une erreur est survenue : {ex.Message}");
                }
            });
        }

        private void Cancel()
        {
            _navigationService.GoBack();
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
                    Client.PropertyChanged += (s, e) => SaveCommand.NotifyCanExecuteChanged();
                }
            });
        }
    }
}