using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ToastNotifications;
using ToastNotifications.Messages;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.WPF.Helpers;

namespace TransitManager.WPF.ViewModels
{
    /// <summary>
    /// ViewModel principal de l'application
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly INavigationService _navigationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly INotificationService _notificationService;
        private readonly Notifier _notifier;

        private Utilisateur? _currentUser;
        private string _searchText = string.Empty;
        private int _notificationCount;
		
		private ObservableObject? _currentView;
        public ObservableObject? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }


        /// <summary>
        /// Utilisateur actuellement connecté
        /// </summary>
        public Utilisateur? CurrentUser
        {
            get => _currentUser;
            set
            {
                if (SetProperty(ref _currentUser, value))
                {
                    OnPropertyChanged(nameof(IsAdmin));
                    OnPropertyChanged(nameof(IsGestionnaire));
                }
            }
        }

        /// <summary>
        /// Texte de recherche globale
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// Nombre de notifications non lues
        /// </summary>
        public int NotificationCount
        {
            get => _notificationCount;
            set
            {
                if (SetProperty(ref _notificationCount, value))
                {
                    OnPropertyChanged(nameof(HasNotifications));
                }
            }
        }

        /// <summary>
        /// Indique s'il y a des notifications
        /// </summary>
        public bool HasNotifications => NotificationCount > 0;

        /// <summary>
        /// Indique si l'utilisateur est administrateur
        /// </summary>
        public bool IsAdmin => CurrentUser?.Role == RoleUtilisateur.Administrateur;

        /// <summary>
        /// Indique si l'utilisateur est gestionnaire ou admin
        /// </summary>
        public bool IsGestionnaire => CurrentUser?.Role == RoleUtilisateur.Administrateur || 
                                      CurrentUser?.Role == RoleUtilisateur.Gestionnaire;

        // Commandes
        public ICommand NavigateCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand NewClientCommand { get; }
        public ICommand NewConteneurCommand { get; }
        public ICommand ScanCommand { get; }
        public ICommand ShowProfileCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ShowNotificationsCommand { get; }

        public MainViewModel(
            IServiceProvider serviceProvider,
            INavigationService navigationService,
            IAuthenticationService authenticationService,
            INotificationService notificationService,
            Notifier notifier)
        {
            _serviceProvider = serviceProvider;
            _navigationService = navigationService;
            _authenticationService = authenticationService;
            _notificationService = notificationService;
            _notifier = notifier;

            Title = "Transit Manager";

            // Initialiser les commandes
            NavigateCommand = new AsyncRelayCommand<string>(NavigateToAsync);
            SearchCommand = new AsyncRelayCommand(PerformSearchAsync);
            NewClientCommand = new RelayCommand(CreateNewClient);
            NewConteneurCommand = new AsyncRelayCommand(CreateNewConteneurAsync);
            ScanCommand = new AsyncRelayCommand(OpenScannerAsync);
            ShowProfileCommand = new AsyncRelayCommand(ShowProfileAsync);
            ShowSettingsCommand = new AsyncRelayCommand(ShowSettingsAsync);
            LogoutCommand = new AsyncRelayCommand(LogoutAsync);
            ShowNotificationsCommand = new AsyncRelayCommand(ShowNotificationsAsync);

            // Charger l'utilisateur connecté
            CurrentUser = _authenticationService.CurrentUser;
			
			if (CurrentUser == null)
			{
				// Simuler l'utilisateur admin pour le développement
				CurrentUser = new Utilisateur
				{
					Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
					NomUtilisateur = "admin",
					Nom = "Administrateur",
					Prenom = "Système",
					Role = RoleUtilisateur.Administrateur
				};
			}

            // S'abonner aux notifications
            _notificationService.NotificationReceived += OnNotificationReceived;
			
			// S'abonner au changement de vue du service de navigation
			_navigationService.CurrentViewChanged += (viewModel) =>
			{
				CurrentView = viewModel;
				_ = viewModel.InitializeAsync(); // Initialiser le nouveau ViewModel
			};
        }

		public override async Task InitializeAsync()
		{
			await base.InitializeAsync();
			
			await LoadNotificationCountAsync();
			_notifier.ShowSuccess($"Bienvenue {CurrentUser?.NomComplet} !");

			// Déclencher la navigation initiale vers le tableau de bord
			NavigateToAsync("Dashboard"); 
		}




		private Task NavigateToAsync(string? viewName)
		{
			if (string.IsNullOrEmpty(viewName)) return Task.CompletedTask;

			return ExecuteBusyActionAsync(() =>
			{
				try
				{
					_navigationService.NavigateTo(viewName);
				}
				catch (Exception ex)
				{
					_notifier.ShowError($"Erreur de navigation : {ex.Message}");
				}
				return Task.CompletedTask;
			});
		}


        private async Task PerformSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return;

            await ExecuteBusyActionAsync(async () =>
            {
                StatusMessage = "Recherche en cours...";
                
                // Naviguer vers la vue de recherche avec le terme de recherche
                _navigationService.NavigateTo("SearchResults", SearchText);
                
                await Task.CompletedTask;
            });
        }

		private void CreateNewClient()
		{
			_navigationService.NavigateTo("ClientDetail", "new");
		}

        private async Task CreateNewConteneurAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                _navigationService.NavigateTo("ConteneurDetail", "new");
                await Task.CompletedTask;
            });
        }

        private async Task OpenScannerAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                _navigationService.NavigateTo("Scanner");
                await Task.CompletedTask;
            });
        }

        private async Task ShowProfileAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                _navigationService.NavigateTo("Profile", CurrentUser?.Id);
                await Task.CompletedTask;
            });
        }

        private async Task ShowSettingsAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                _navigationService.NavigateTo("Settings");
                await Task.CompletedTask;
            });
        }

        private async Task LogoutAsync()
        {
            var result = await _serviceProvider.GetRequiredService<IDialogService>()
                .ShowConfirmationAsync("Déconnexion", "Êtes-vous sûr de vouloir vous déconnecter ?");

            if (result)
            {
                await ExecuteBusyActionAsync(async () =>
                {
                    StatusMessage = "Déconnexion en cours...";
                    
                    await _authenticationService.LogoutAsync();
                    
                    // Redémarrer l'application
                    System.Diagnostics.Process.Start(Environment.ProcessPath!);
                    Environment.Exit(0);
                });
            }
        }

        private async Task ShowNotificationsAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                _navigationService.NavigateTo("Notifications");
                await Task.CompletedTask;
            });
        }

        private async Task LoadNotificationCountAsync()
        {
            try
            {
                NotificationCount = await _notificationService.GetUnreadCountAsync();
            }
            catch (Exception ex)
            {
                _notifier.ShowError($"Erreur lors du chargement des notifications : {ex.Message}");
            }
        }

        private void OnNotificationReceived(object? sender, NotificationEventArgs e)
        {
            // Afficher la notification toast
            switch (e.Type)
            {
                case TypeNotification.Information:
                    _notifier.ShowInformation(e.Message);
                    break;
                case TypeNotification.Succes:
                    _notifier.ShowSuccess(e.Message);
                    break;
                case TypeNotification.Avertissement:
                    _notifier.ShowWarning(e.Message);
                    break;
                case TypeNotification.Erreur:
                    _notifier.ShowError(e.Message);
                    break;
                default:
                    _notifier.ShowInformation(e.Message);
                    break;
            }

            // Mettre à jour le compteur
            NotificationCount++;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notificationService.NotificationReceived -= OnNotificationReceived;
            }
            base.Dispose(disposing);
        }
    }
}