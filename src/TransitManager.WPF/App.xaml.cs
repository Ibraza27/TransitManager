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
            base.OnStartup(e);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/transitmanager-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Démarrage de Transit Manager");

                _host = Host.CreateDefaultBuilder(e.Args)
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.SetBasePath(Directory.GetCurrentDirectory())
                              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    })
                    .ConfigureServices((context, services) => ConfigureServices(context.Configuration, services))
                    .UseSerilog()
                    .Build();

                await _host.StartAsync();
                _notifier = _host.Services.GetRequiredService<Notifier>();

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                Current.MainWindow = mainWindow;

                if (mainWindow.DataContext is MainViewModel mainViewModel)
                {
                    await mainViewModel.InitializeAsync();
                }

                mainWindow.Show();
            }
			catch (Exception ex)
			{
				Log.Fatal(ex, "Une erreur fatale s'est produite lors du démarrage");
				System.Windows.MessageBox.Show($"Une erreur critique est survenue: {ex.Message}\n\n{ex.InnerException?.Message}", "Erreur de Démarrage", MessageBoxButton.OK, MessageBoxImage.Error);
				Shutdown(-1);
			}
        }

        private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            services.AddSingleton(configuration);

            // Base de données : Utilisation de la DbContextFactory, la meilleure pratique pour WPF.
            services.AddDbContextFactory<TransitContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Repositories (peuvent rester Scoped ou passer en Transient, Transient est plus sûr)
            services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddTransient<IClientRepository, ClientRepository>();
            services.AddTransient<IColisRepository, ColisRepository>();
            services.AddTransient<IConteneurRepository, ConteneurRepository>();

            // Services métier (doivent être Transient car ils dépendent de la DbContextFactory)
            services.AddTransient<IClientService, ClientService>();
            services.AddTransient<IColisService, ColisService>();
			services.AddTransient<IVehiculeService, VehiculeService>();
            services.AddTransient<IConteneurService, ConteneurService>();
            services.AddTransient<IPaiementService, PaiementService>();
            services.AddTransient<INotificationService, NotificationService>();
            services.AddTransient<IAuthenticationService, AuthenticationService>();
            
            // Services d'infrastructure (Scoped est ok ici car pas de DbContext)
            services.AddScoped<IBarcodeService, BarcodeService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddScoped<IBackupService, BackupService>();
			
			services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default);

            // Services UI (Singleton car ils gèrent un état global de l'UI)
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();

            // AutoMapper
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // ViewModels (Transient pour qu'ils soient recréés à chaque navigation)
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

            // Views (Transient)
            services.AddTransient<LoginView>();
            services.AddTransient<MainWindow>();
			services.AddTransient<DetailHostWindow>();
			services.AddTransient<AddColisToConteneurView>();
			services.AddTransient<AddVehiculeToConteneurView>();
			services.AddTransient<Views.Paiements.PaiementColisView>();
			services.AddTransient<Views.Paiements.PaiementVehiculeView>();
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