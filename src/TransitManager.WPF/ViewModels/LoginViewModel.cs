using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using TransitManager.Core.Enums;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TransitManager.Core.Interfaces;
using System.ComponentModel;

namespace TransitManager.WPF.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authenticationService;
		public Action<bool?>? CloseAction { get; set; }
        //private readonly Window _window;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _rememberMe;
        private string _errorMessage = string.Empty;
        private bool _hasError;
        private string? _appVersion;
		

		public string Username
		{
			get => _username;
			set
			{
				if (SetProperty(ref _username, value))
				{
					HasError = false;
					RaiseCanExecuteChanged(); // ⭐ Notifier le changement ⭐
				}
			}
		}

		public string Password
		{
			get => _password;
			set
			{
				if (SetProperty(ref _password, value))
				{
					HasError = false;
					RaiseCanExecuteChanged(); // ⭐ Notifier le changement ⭐
				}
			}
		}

        public bool RememberMe
        {
            get => _rememberMe;
            set => SetProperty(ref _rememberMe, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public string? AppVersion
        {
            get => _appVersion;
            set => SetProperty(ref _appVersion, value);
        }

        // Commandes
        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordCommand { get; }

		public LoginViewModel(IAuthenticationService authenticationService)
		{
			_authenticationService = authenticationService;

			Title = "Connexion";
			AppVersion = GetAppVersion();
			IsBusy = false; // ⭐ 1. Initialiser explicitement ⭐

			// Initialiser les commandes
			LoginCommand = new AsyncRelayCommand(LoginAsync, () => CanLogin());
  			RegisterCommand(nameof(LoginCommand), (IRelayCommand)LoginCommand);
			ForgotPasswordCommand = new AsyncRelayCommand(ForgotPasswordAsync);

			// Charger les préférences sauvegardées
			LoadSavedCredentials();
		}

		// ⭐ 3. Implémenter INotifyPropertyChanged pour CanExecute ⭐
		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			base.OnPropertyChanged(e);
			
			if (e.PropertyName == nameof(Username) || 
				e.PropertyName == nameof(Password) ||
				e.PropertyName == nameof(IsBusy))
			{
				(LoginCommand as RelayCommand)?.NotifyCanExecuteChanged();
			}
		}

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) && 
                   !string.IsNullOrWhiteSpace(Password) && 
                   !IsBusy;
        }

        private async Task LoginAsync()
        {
            await ExecuteBusyActionAsync(async () =>
            {
                StatusMessage = "Connexion en cours...";
                HasError = false;
                ErrorMessage = string.Empty;

                try
                {
                    var result = await _authenticationService.LoginAsync(Username, Password);

                    if (result.Success)
                    {
                        // Sauvegarder les identifiants si demandé
                        if (RememberMe)
                        {
                            SaveCredentials();
                        }
                        else
                        {
                            ClearSavedCredentials();
                        }

                        // Si l'utilisateur doit changer son mot de passe
                        if (result.RequiresPasswordChange)
                        {
                            // TODO: Afficher la vue de changement de mot de passe
                            System.Windows.MessageBox.Show(
                                "Vous devez changer votre mot de passe.",
                                "Changement de mot de passe requis",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }

                        // Fermer la fenêtre de connexion avec succès
						CloseAction?.Invoke(true);
                    }
                    else
                    {
                        HasError = true;
                        ErrorMessage = result.ErrorMessage ?? "Échec de la connexion.";
                        
                        // Secouer la fenêtre pour indiquer l'erreur
                        ShakeWindow();
                    }
                }
                catch (Exception ex)
                {
                    HasError = true;
                    ErrorMessage = $"Erreur de connexion : {ex.Message}";
                }
            });
        }

        private async Task ForgotPasswordAsync()
        {
            // TODO: Implémenter la récupération de mot de passe
            await Task.CompletedTask;
            
            System.Windows.MessageBox.Show(
                "Un email de réinitialisation sera envoyé à l'adresse associée à votre compte.\n\nCette fonctionnalité sera bientôt disponible.",
                "Mot de passe oublié",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void LoadSavedCredentials()
        {
            try
            {
                var config = ConfigurationManager.AppSettings;
                var savedUsername = config["SavedUsername"];
                var rememberMe = config["RememberMe"];

                if (!string.IsNullOrEmpty(rememberMe) && bool.Parse(rememberMe))
                {
                    Username = savedUsername ?? string.Empty;
                    RememberMe = true;
                }
            }
            catch
            {
                // Ignorer les erreurs de chargement
            }
        }

        private void SaveCredentials()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["SavedUsername"].Value = Username;
                config.AppSettings.Settings["RememberMe"].Value = "true";
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch
            {
                // Ignorer les erreurs de sauvegarde
            }
        }

        private void ClearSavedCredentials()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["SavedUsername"].Value = string.Empty;
                config.AppSettings.Settings["RememberMe"].Value = "false";
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch
            {
                // Ignorer les erreurs
            }
        }

        private string GetAppVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }

        private void ShakeWindow()
        {
            /*
			if (_window == null) return;

            var originalLeft = _window.Left;
            var shakeAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 10,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(3)
            };

            var transform = new System.Windows.Media.TranslateTransform();
            _window.RenderTransform = transform;

            shakeAnimation.Completed += (s, e) =>
            {
                _window.RenderTransform = null;
                _window.Left = originalLeft;
            };

            transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, shakeAnimation);
			*/
        }
    }
}