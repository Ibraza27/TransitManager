using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using TransitManager.Infrastructure.Repositories;
using TransitManager.Infrastructure.Services;
using TransitManager.WPF.Helpers;
using TransitManager.WPF.ViewModels;
using TransitManager.WPF.Views.Auth;
using TransitManager.WPF.Views.Clients; // Ajoutez les using pour les vues
using TransitManager.WPF.Views.Colis;
using TransitManager.WPF.Views.Conteneurs;
using TransitManager.WPF.Views.Dashboard;
using TransitManager.WPF.Views.Finance;

namespace TransitManager.WPF
{
    public partial class App : System.Windows.Application
    {
        private IHost? _host;
        // La variable Notifier est conservée ici
        private Notifier? _notifier;

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

                // On récupère le Notifier depuis le conteneur DI
				var mainWindow = _host.Services.GetRequiredService<MainWindow>();
				Current.MainWindow = mainWindow; 

                // Assurez-vous que le DataContext de la MainWindow est défini sur le MainViewModel
                // et que le MainViewModel est configuré pour afficher le DashboardViewModel au démarrage.
                if (mainWindow.DataContext is MainViewModel mainViewModel)
                {
                    // Initialiser le MainViewModel
                    await mainViewModel.InitializeAsync(); 
                }

				mainWindow.Show();



            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Une erreur fatale s'est produite lors du démarrage");
                System.Windows.MessageBox.Show($"Une erreur critique est survenue: {ex.Message}", "Erreur de Démarrage", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            // Configuration
            services.AddSingleton(configuration);

			// Base de données
			// On passe en Transient pour créer une nouvelle instance à chaque fois, évitant les problèmes de thread.
			services.AddDbContext<TransitContext>(options =>
				options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")),
				ServiceLifetime.Transient);

			// Repositories
			services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
			services.AddTransient<IClientRepository, ClientRepository>();
			services.AddTransient<IColisRepository, ColisRepository>();
			services.AddTransient<IConteneurRepository, ConteneurRepository>();

			// Services métier
			services.AddTransient<IClientService, ClientService>();
			services.AddTransient<IColisService, ColisService>();
			services.AddTransient<IConteneurService, ConteneurService>();
			services.AddTransient<IPaiementService, PaiementService>();
			services.AddTransient<INotificationService, NotificationService>();
			services.AddTransient<IBarcodeService, BarcodeService>();
						
			// Services infrastructure
			services.AddTransient<IBackupService, BackupService>();
			services.AddTransient<IExportService, ExportService>();
						
			// AuthenticationService
			services.AddTransient<IAuthenticationService, AuthenticationService>();

            // CORRECTION : La navigation et les dialogues sont liés à l'UI, Singleton est OK ici.
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();

            // AutoMapper
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

			// ViewModels
			services.AddTransient<LoginViewModel>();
			services.AddTransient<MainViewModel>();
			services.AddTransient<DashboardViewModel>();
			services.AddTransient<ClientViewModel>();
			services.AddTransient<ClientDetailViewModel>(); // <-- AJOUTEZ CETTE LIGNE
			services.AddTransient<ColisViewModel>();
			services.AddTransient<ConteneurViewModel>();
			services.AddTransient<ConteneurDetailViewModel>();
			services.AddTransient<NotificationsViewModel>();
						
            // Views
            services.AddTransient<LoginView>();
            services.AddTransient<MainWindow>();
			
			services.AddTransient<DashboardView>();
			services.AddTransient<ClientListView>();
			services.AddTransient<ClientDetailView>();
			services.AddTransient<ColisListView>();
			services.AddTransient<ColisScanView>();
			services.AddTransient<ConteneurListView>();
			services.AddTransient<ConteneurDetailView>();
			services.AddTransient<PaiementView>();
			services.AddTransient<FactureView>();

            // CORRECTION : On enregistre le Notifier pour qu'il soit injectable partout
			// Notification service
			services.AddSingleton(provider =>
			{
				// On crée le Notifier ici, à la demande.
				return new Notifier(cfg =>
				{
					cfg.PositionProvider = new PrimaryScreenPositionProvider(
						corner: Corner.TopRight,
						offsetX: 10,
						offsetY: 10);

					cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
						notificationLifetime: TimeSpan.FromSeconds(5),
						maximumNotificationCount: MaximumNotificationCount.FromCount(5));

					cfg.Dispatcher = System.Windows.Application.Current.Dispatcher;
				});
			});
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

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Exception non gérée");
            System.Windows.MessageBox.Show($"Une erreur inattendue est survenue:\n{e.Exception.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Log.Fatal(exception, "Erreur fatale");
        }
    }
}