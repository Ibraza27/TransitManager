using Microsoft.Extensions.Logging;
using Refit;
using TransitManager.Mobile.Services;
using TransitManager.Mobile.ViewModels;
using TransitManager.Mobile.Views;
using System.Text.Json; // <-- AJOUTER CE USING
using System.Text.Json.Serialization; // <-- AJOUTER CE USING

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
            string baseApiUrl = "https://100.91.147.96:7243"; 
			

            // Ajout du client Refit pour communiquer avec l'API
            // Créer des options de sérialisation JSON partagées pour le client
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                // Indique au client de comprendre les métadonnées ($id, $ref)
                ReferenceHandler = ReferenceHandler.Preserve,
                // Il est bon de s'assurer que le client et le serveur gèrent les noms de la même manière
                PropertyNameCaseInsensitive = true
            };

            // Créer les paramètres Refit avec notre sérialiseur personnalisé
            var refitSettings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
            };

            // Ajout du client Refit pour communiquer avec l'API
            builder.Services.AddRefitClient<ITransitApi>(refitSettings) // <-- On passe les paramètres ici
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
            
            // --- AJOUTER CES LIGNES ---
            builder.Services.AddTransient<ClientDetailPage>();
            builder.Services.AddTransient<ClientDetailViewModel>();
            
            // --- FIN DES AJOUTS ---
			// --- AJOUTER CES LIGNES ---
			builder.Services.AddTransient<AddEditClientPage>();
			builder.Services.AddTransient<AddEditClientViewModel>();

			builder.Services.AddSingleton<ColisPage>();
			builder.Services.AddSingleton<ColisViewModel>();
			
			builder.Services.AddTransient<ColisDetailPage>();
			builder.Services.AddTransient<ColisDetailViewModel>();
			
			builder.Services.AddTransient<AddEditColisPage>();
			builder.Services.AddTransient<AddEditColisViewModel>();
			
            return builder.Build();
        }
    }
}