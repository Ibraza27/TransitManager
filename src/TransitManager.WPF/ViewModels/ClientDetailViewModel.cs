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
using TransitManager.WPF.Services;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection; // Assurez-vous d'avoir ce using
using TransitManager.WPF.Views; // Et celui-ci
using System.Windows; // <-- AJOUTER CETTE LIGNE

namespace TransitManager.WPF.ViewModels
{
    public class ClientDetailViewModel : BaseViewModel
    {
        private readonly IClientService _clientService;
        private readonly INavigationService _navigationService;
        private readonly IDialogService _dialogService;
		private readonly IMessenger _messenger;
		private readonly IApiClient _apiClient;
		private readonly IServiceProvider _serviceProvider;
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

        public IAsyncRelayCommand CreateOrResetPortalAccessCommand { get; } // <-- AJOUTER
		public ICommand GoToPortalCommand { get; }

		public ClientDetailViewModel(IClientService clientService, INavigationService navigationService, IDialogService dialogService, IMessenger messenger, IApiClient apiClient, IServiceProvider serviceProvider)
		{
            _clientService = clientService;
            _navigationService = navigationService;
            _dialogService = dialogService;
            _messenger = messenger;
            _apiClient = apiClient; // <-- AJOUTER
			_serviceProvider = serviceProvider;
			GoToPortalCommand = new AsyncRelayCommand(GoToPortalAsync);

            SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            
            // === AJOUTER CETTE LIGNE ===
            CreateOrResetPortalAccessCommand = new AsyncRelayCommand(CreateOrResetPortalAccess, CanCreatePortalAccess);
            GoToPortalCommand = new AsyncRelayCommand(GoToPortalAsync);
        }
		
		
		private async Task GoToPortalAsync()
		{
			if (Client?.UserAccount == null) return;

			// --- NOUVELLE LOGIQUE DE NAVIGATION ---
			using var scope = _serviceProvider.CreateScope();
			var userDetailViewModel = scope.ServiceProvider.GetRequiredService<UserDetailViewModel>();
			
			userDetailViewModel.SetModalMode();

			// On initialise le ViewModel avec l'ID de l'utilisateur lié
			await userDetailViewModel.InitializeAsync(Client.UserAccount.Id);

			if (userDetailViewModel.User == null)
			{
				await _dialogService.ShowErrorAsync("Erreur", "Impossible de charger les détails du compte utilisateur.");
				return;
			}

			var window = new DetailHostWindow
			{
				DataContext = userDetailViewModel,
                Owner = System.Windows.Application.Current.MainWindow,
                Title = $"Détails du Compte - {userDetailViewModel.User.NomUtilisateur}"
			};
			
			userDetailViewModel.CloseAction = () => window.Close();

			window.ShowDialog();
			
			// Après la fermeture, on rafraîchit la fiche client au cas où quelque chose aurait changé
			await InitializeAsync(Client.Id);
		}

        private bool CanCreatePortalAccess()
        {
            // On peut créer un accès si le client a un email et a déjà été sauvegardé (a un Id)
            return Client != null && Client.Id != Guid.Empty && !string.IsNullOrEmpty(Client.Email);
        }
        
        private async Task CreateOrResetPortalAccess()
        {
            if (!CanCreatePortalAccess() || Client == null)
            {
                await _dialogService.ShowWarningAsync("Action impossible", "Veuillez enregistrer le client et lui assigner un email avant de créer un accès portail.");
                return;
            }

            await ExecuteBusyActionAsync(async () =>
            {
                try
                {
					var result = await _apiClient.CreateOrResetPortalAccess(Client.Id);
					
					await _dialogService.ShowInformationAsync(
						"Accès créé avec succès",
						$"Le compte pour le client a été créé/mis à jour.\n\n" +
						$"Nom d'utilisateur : {result.Username}\n" +
						$"Mot de passe temporaire : {result.TemporaryPassword}\n\n" +
						"Veuillez communiquer ces informations au client.");

					// === CORRECTION : RAFRAÎCHIR LES DONNÉES ===
					// On recharge le client depuis la base de données pour obtenir
					// la nouvelle propriété de navigation 'UserAccount'.
					await InitializeAsync(Client.Id);
					// === FIN DE LA CORRECTION ===
				}
				catch (Exception ex)
				{
					await _dialogService.ShowErrorAsync("Erreur", $"Impossible de créer l'accès portail : {ex.Message}");
				}
			});
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
				Client.PropertyChanged += (s, e) => 
				{
					SaveCommand.NotifyCanExecuteChanged();
					CreateOrResetPortalAccessCommand.NotifyCanExecuteChanged();
				};
			}
		}

		public async Task InitializeAsync(Guid clientId)
		{
			await ExecuteBusyActionAsync(async () =>
			{
				// === DÉBUT DE LA CORRECTION ===
				var clientData = await _clientService.GetByIdAsync(clientId); 
				if (clientData != null)
				{
					Client = clientData; // Assigner les données à la propriété
					Title = $"Modifier - {Client.NomComplet}";
					_isNewClient = false;
					
					Client.PropertyChanged += (s, e) => 
					{
						SaveCommand.NotifyCanExecuteChanged();
						CreateOrResetPortalAccessCommand.NotifyCanExecuteChanged();
					};
					
					// Forcer la notification pour que l'UI se mette à jour avec les nouvelles données
					OnPropertyChanged(nameof(Client)); 
					OnPropertyChanged(nameof(ImpayesColis));
					OnPropertyChanged(nameof(ImpayesVehicules));
					CreateOrResetPortalAccessCommand.NotifyCanExecuteChanged();
				}
				// === FIN DE LA CORRECTION ===
			});
		}
	}
}