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

                // On récupère le Notifier depuis le conteneur DI
                _notifier = _host.Services.GetRequiredService<Notifier>();

                Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

                // Afficher la fenêtre de connexion
                var loginWindow = _host.Services.GetRequiredService<LoginView>();
                var loginResult = loginWindow.ShowDialog();

                if (loginResult == true)
                {
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    Current.MainWindow = mainWindow; // CORRECTION : Définir la MainWindow principale
                    mainWindow.Show();
                }
                else
                {
                    Shutdown();
                }
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
            services.AddDbContext<TransitContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

            // Repositories
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IColisRepository, ColisRepository>();
            services.AddScoped<IConteneurRepository, ConteneurRepository>();

            // Services métier
            services.AddScoped<IClientService, ClientService>();
            services.AddScoped<IColisService, ColisService>();
            services.AddScoped<IConteneurService, ConteneurService>();
            services.AddScoped<IPaiementService, PaiementService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IBarcodeService, BarcodeService>();
            
            // Services infrastructure
            services.AddScoped<IBackupService, BackupService>();
            services.AddScoped<IExportService, ExportService>();
            
            // CORRECTION : Mettre AuthenticationService en Scoped car il dépend du TransitContext (Scoped)
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            // CORRECTION : La navigation et les dialogues sont liés à l'UI, Singleton est OK ici.
            services.AddScoped<INavigationService, NavigationService>();
            services.AddSingleton<IDialogService, DialogService>();

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
            services.AddTransient<MainWindow>();

            // CORRECTION : On enregistre le Notifier pour qu'il soit injectable partout
            services.AddSingleton(provider => new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: System.Windows.Application.Current.MainWindow,
                    corner: Corner.TopRight,
                    offsetX: 10,
                    offsetY: 10);
                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(5),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));
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