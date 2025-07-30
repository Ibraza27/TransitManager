using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Core;
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

namespace TransitManager.WPF
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private IHost? _host;
        private Notifier? _notifier;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configuration de Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("logs/transitmanager-.txt", 
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Démarrage de Transit Manager");

                // Configuration du host
                _host = Host.CreateDefaultBuilder(e.Args)
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.SetBasePath(Directory.GetCurrentDirectory())
                              .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                              .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                              .AddEnvironmentVariables()
                              .AddCommandLine(e.Args);
                    })
                    .ConfigureServices((context, services) => ConfigureServices(context.Configuration, services))
                    .UseSerilog()
                    .Build();

                await _host.StartAsync();

                // Configuration des notifications Toast
                _notifier = new Notifier(cfg =>
                {
                    cfg.PositionProvider = new WindowPositionProvider(
                        parentWindow: Current.MainWindow,
                        corner: Corner.TopRight,
                        offsetX: 10,
                        offsetY: 10);

                    cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                        notificationLifetime: TimeSpan.FromSeconds(5),
                        maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                    cfg.Dispatcher = Current.Dispatcher;
                });

                // Gestion globale des exceptions
                Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                // Afficher la fenêtre de connexion
                var loginWindow = _host.Services.GetRequiredService<LoginView>();
                var loginResult = loginWindow.ShowDialog();

                if (loginResult == true)
                {
                    // L'utilisateur s'est connecté avec succès, afficher la fenêtre principale
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    mainWindow.Show();
                }
                else
                {
                    // L'utilisateur a annulé la connexion
                    Shutdown();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Une erreur fatale s'est produite lors du démarrage de l'application");
                System.Windows.MessageBox.Show(
                    $"Une erreur s'est produite lors du démarrage de l'application:\n{ex.Message}",
                    "Erreur de démarrage",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void ConfigureServices(IConfiguration configuration, IServiceCollection services)
        {
            // Configuration
            services.AddSingleton(configuration);

            // Base de données
            services.AddDbContext<TransitContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IColisRepository, ColisRepository>();
            services.AddScoped<IConteneurRepository, ConteneurRepository>();


			// Services métier
			services.AddScoped<IClientService, TransitManager.Infrastructure.Services.ClientService>();
			services.AddScoped<IColisService, TransitManager.Infrastructure.Services.ColisService>();
			services.AddScoped<IConteneurService, TransitManager.Infrastructure.Services.ConteneurService>();
			services.AddScoped<IPaiementService, TransitManager.Infrastructure.Services.PaiementService>();
			services.AddScoped<INotificationService, TransitManager.Infrastructure.Services.NotificationService>();
			services.AddScoped<IBarcodeService, TransitManager.Infrastructure.Services.BarcodeService>();
			
			
            // Services infrastructure
            services.AddScoped<IBackupService, BackupService>();
            services.AddScoped<IExportService, ExportService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // SignalR Hub

            // AutoMapper
            services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

            // ViewModels
            services.AddTransient<LoginViewModel>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<ClientViewModel>();
            services.AddTransient<ColisViewModel>();
            services.AddTransient<ConteneurViewModel>();

            // Views
            services.AddTransient<LoginView>();
            services.AddSingleton<MainWindow>();

            // Notification service
            services.AddSingleton(_notifier!);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log.Information("Arrêt de Transit Manager");

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
            Log.Error(e.Exception, "Exception non gérée dans le dispatcher");

            System.Windows.MessageBox.Show(
                $"Une erreur inattendue s'est produite:\n{e.Exception.Message}\n\nL'application va continuer à fonctionner.",
                "Erreur",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            Log.Fatal(exception, "Exception non gérée dans l'application");

            System.Windows.MessageBox.Show(
                $"Une erreur fatale s'est produite:\n{exception?.Message}\n\nL'application doit être fermée.",
                "Erreur fatale",
                MessageBoxButton.OK,
                MessageBoxImage.Stop);
        }
    }
}