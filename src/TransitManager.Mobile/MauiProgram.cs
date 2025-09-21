using Microsoft.Extensions.Logging;
using Refit;
using TransitManager.Mobile.Services;
using TransitManager.Mobile.ViewModels;
using TransitManager.Mobile.Views;

namespace TransitManager.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

    #if DEBUG
            builder.Logging.AddDebug();
    #endif
            
            // --- DÉBUT DES AJOUTS ---

            // Configuration de l'URL de base de l'API
            // IMPORTANT : Sur l'émulateur Android, localhost est 10.0.2.2
            // Sur un appareil physique, vous devrez mettre l'adresse IP de votre ordinateur sur le réseau local.
            string baseApiUrl = DeviceInfo.Platform == DevicePlatform.Android 
                ? "https://10.0.2.2:7001" 
                : "https://localhost:7001";

            // Ajout du client Refit pour communiquer avec l'API
            builder.Services.AddRefitClient<ITransitApi>()
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseApiUrl))
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    // ATTENTION : Uniquement pour le développement.
                    // Permet d'ignorer les erreurs de certificat SSL auto-signé de Kestrel.
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                });

            // Injection des Vues et ViewModels
            builder.Services.AddSingleton<ClientsPage>();
            builder.Services.AddSingleton<ClientsViewModel>();
            
            // --- FIN DES AJOUTS ---

            return builder.Build();
        }
    }
}