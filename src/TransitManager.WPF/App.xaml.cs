using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Windows;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using TransitManager.Infrastructure.Repositories;
using TransitManager.Infrastructure.Services;
using TransitManager.WPF.Helpers;
using TransitManager.WPF.ViewModels;
using TransitManager.WPF.Views;
using TransitManager.WPF.Views.Auth;
using TransitManager.WPF.Views.Clients;
using TransitManager.WPF.Views.Colis;
using TransitManager.WPF.Views.Conteneurs;
using TransitManager.WPF.Views.Dashboard;
using TransitManager.WPF.Views.Finance;
using TransitManager.WPF.Views.Notifications;
using System.Globalization; 
using System.Threading;    
using System.Windows.Markup;
using TransitManager.WPF.Views.Users;
using TransitManager.WPF.Services; 
using System.Threading.Tasks;

namespace TransitManager.WPF
{
    public partial class App : System.Windows.Application
    {
        private IHost? _host;
        private Notifier? _notifier;
		
        public App()
        {
            // Définit la culture pour toute l'application
            var culture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            // 1. Afficher le Splash Screen immédiatement
            // On désactive temporairement le ShutdownMode pour que la fermeture du Splash ne tue pas l'app
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splashVm = new ViewModels.SplashViewModel();
            var splashScreen = new SplashView { DataContext = splashVm };
            splashScreen.Show();

            // Logger Configuration (On le garde ici ou on le met dans la Task, peu importe, c'est rapide)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/transitmanager-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Démarrage de Transit Manager (Splash Screen)...");

                // 2. Lancer l'initialisation lourde en arrière-plan
                // Cela permet à l'UI du Splash Screen (le spinner) de tourner de manière fluide
                await Task.Run(async () =>
                {
                    UpdateStatus(splashVm, "Configuration des services...");
                    
                    // Construction de l'hôte
                    _host = Host.CreateDefaultBuilder(e.Args)
                        .ConfigureAppConfiguration((context, config) =>
                        {
                            config.SetBasePath(Directory.GetCurrentDirectory())
                                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                  .AddJsonFile("appsettings.override.json", optional: true, reloadOnChange: true);
                        })
                        .ConfigureServices((context, services) => ConfigureServices(context.Configuration, services))
                        .UseSerilog()
                        .Build();

                    UpdateStatus(splashVm, "Démarrage des services...");
                    await _host.StartAsync();
                    
                    // Récupération des services UI (doit être fait sur le thread principal plus tard, 
                    // mais on prépare ce qu'on peut ici)
                });

                // 3. Initialisation des composants UI sur le thread principal
                _notifier = _host.Services.GetRequiredService<Notifier>();

                splashVm.LoadingStatus = "Connexion au serveur...";
                var signalRClient = _host.Services.GetRequiredService<SignalRClientService>();
                // On lance la connexion mais on n'attend pas forcément qu'elle soit finie pour afficher l'app
                // ou on l'attend si c'est critique :
                await signalRClient.StartAsync();

                splashVm.LoadingStatus = "Préparation de l'interface...";
                
                // Création de la fenêtre principale
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                
                // Initialisation du ViewModel principal
                if (mainWindow.DataContext is MainViewModel mainViewModel)
                {
                    await mainViewModel.InitializeAsync();
                }

                // 4. Transition
                // On remet le mode d'arrêt normal (quand la fenêtre principale ferme, l'app ferme)
                this.ShutdownMode = ShutdownMode.OnMainWindowClose;
                this.MainWindow = mainWindow;
                
                mainWindow.Show();
                splashScreen.Close();
            }
            catch (Exception ex)
            {
                splashScreen.Close(); // Fermer le splash en cas d'erreur
                Log.Fatal(ex, "Une erreur fatale s'est produite lors du démarrage");
                System.Windows.MessageBox.Show($"Une erreur critique est survenue lors du démarrage :\n{ex.Message}\n\n{ex.InnerException?.Message}", "Erreur de Démarrage", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        // Petite méthode utilitaire pour mettre à jour le texte du splash depuis un thread background
        private void UpdateStatus(ViewModels.SplashViewModel vm, string message)
        {
            Dispatcher.Invoke(() => vm.LoadingStatus = message);
        }

		private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
		{
			services.AddSingleton(configuration);

			// VÉRIFIEZ BIEN QUE CETTE LIGNE EST PRÉSENTE ET QUE AddDbContext N'EST PLUS LÀ
			services.AddDbContextFactory<TransitContext>(options =>
				options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

			// VÉRIFIEZ QUE VOS SERVICES ET REPOSITORIES SONT BIEN ENREGISTRÉS EN TRANSIENT
			services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
			services.AddTransient<IClientRepository, ClientRepository>();
			services.AddTransient<IColisRepository, ColisRepository>();
			services.AddTransient<IConteneurRepository, ConteneurRepository>();

			// 3. Services métier : Doivent être Transient pour garantir une nouvelle instance à chaque fois.
			services.AddTransient<IClientService, ClientService>();
			services.AddTransient<IColisService, ColisService>();
			services.AddTransient<IVehiculeService, VehiculeService>();
			services.AddTransient<IConteneurService, ConteneurService>();
			services.AddTransient<IPaiementService, PaiementService>();
			services.AddTransient<IAuthenticationService, AuthenticationService>();
			services.AddTransient<IUserService, UserService>();
			
			// Les services sans état ou qui gèrent des connexions peuvent rester Singleton
			services.AddSingleton<INotificationHubService, NotificationHubService>(); 
			services.AddSingleton<INotificationService, NotificationService>(); // Supposant qu'il gère des événements globaux

			// 4. Services d'infrastructure
			services.AddTransient<IBarcodeService, BarcodeService>();
			services.AddTransient<IExportService, ExportService>();
			services.AddTransient<IBackupService, BackupService>();
			services.AddTransient<IPrintingService, PrintingService>();
			
			services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default);

            // Services UI (Singleton car ils gèrent un état global de l'UI)
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();
			services.AddSingleton<SignalRClientService>();
			
			// === DÉBUT DE LA CORRECTION ===
			// On récupère la clé secrète depuis la configuration
			string? internalSecret = configuration["InternalSecret"];
			if (string.IsNullOrEmpty(internalSecret))
			{
				throw new InvalidOperationException("La clé secrète interne 'InternalSecret' n'est pas configurée dans appsettings.json.");
			}

			services.AddHttpClient("API", client =>
			{
				client.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "https://localhost:7243");
				// On ajoute l'en-tête par défaut à ce client nommé
				client.DefaultRequestHeaders.Add("X-Internal-Secret", internalSecret);
			});
			
			// On enregistre IApiClient pour qu'il utilise le client HTTP configuré ci-dessus.
			services.AddScoped<IApiClient, ApiClient>();
			// === FIN DE LA CORRECTION ===

			// AutoMapper
			services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

			// ViewModels : Doivent être Transient.
			services.AddTransient<LoginViewModel>();
			services.AddTransient<MainViewModel>();
			services.AddTransient<DashboardViewModel>();
			services.AddTransient<ClientViewModel>();
			services.AddTransient<ClientDetailViewModel>();
			services.AddTransient<ColisViewModel>();
			services.AddTransient<ColisDetailViewModel>();
			services.AddTransient<VehiculeViewModel>();
			services.AddTransient<VehiculeDetailViewModel>();
			services.AddTransient<ConteneurViewModel>();
			services.AddTransient<ConteneurDetailViewModel>();
			services.AddTransient<AddColisToConteneurViewModel>();
			services.AddTransient<AddVehiculeToConteneurViewModel>();
			services.AddTransient<NotificationsViewModel>();
			services.AddTransient<PaiementColisViewModel>();
			services.AddTransient<PaiementVehiculeViewModel>();
			services.AddTransient<PrintPreviewViewModel>();
			services.AddTransient<FinanceViewModel>();
			services.AddTransient<UserViewModel>();
			services.AddTransient<UserDetailViewModel>();

            // Views (Transient)
            services.AddTransient<LoginView>();
            services.AddTransient<MainWindow>();
			services.AddTransient<DetailHostWindow>();
			services.AddTransient<AddColisToConteneurView>();
			services.AddTransient<AddVehiculeToConteneurView>();
			services.AddTransient<Views.Paiements.PaiementColisView>();
			services.AddTransient<Views.Paiements.PaiementVehiculeView>();
			services.AddTransient<PrintPreviewView>();
            services.AddTransient<DashboardView>();
            services.AddTransient<ClientListView>();
            services.AddTransient<ClientDetailView>();
            services.AddTransient<ColisListView>();
            services.AddTransient<ColisScanView>();
			services.AddTransient<Views.Vehicules.VehiculeListView>();
			services.AddTransient<Views.Vehicules.VehiculeDetailView>();
            services.AddTransient<ConteneurListView>();
            services.AddTransient<ConteneurDetailView>();
            services.AddTransient<PaiementView>();
            services.AddTransient<FactureView>();
            services.AddTransient<NotificationsView>();
			services.AddTransient<FinanceView>();
			services.AddTransient<UserListView>();
			services.AddTransient<UserDetailView>();

            // Service de Notification (Toast)
            services.AddSingleton(provider => new Notifier(cfg =>
            {
                cfg.PositionProvider = new PrimaryScreenPositionProvider(corner: Corner.TopRight, offsetX: 10, offsetY: 10);
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(notificationLifetime: TimeSpan.FromSeconds(5), maximumNotificationCount: MaximumNotificationCount.FromCount(5));
                cfg.Dispatcher = System.Windows.Application.Current.Dispatcher;
            }));
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            _notifier?.Dispose();
            if (_host != null)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
                _host.Dispose();
            }
            Log.CloseAndFlush();
            base.OnExit(e);
        }

		private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			Log.Error(e.Exception, "Exception non gérée");
			System.Windows.MessageBox.Show($"Une erreur inattendue est survenue:\n{e.Exception.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}
    }
}