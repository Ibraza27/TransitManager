using Microsoft.Extensions.Logging;
using Refit;
using TransitManager.Mobile.Services;
using TransitManager.Mobile.ViewModels;
using TransitManager.Mobile.Views;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Microsoft.Extensions.Configuration; // <-- ASSUREZ-VOUS QUE CE USING EST LÀ

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

            // --- DÉBUT DE LA MODIFICATION ---
			var assembly = Assembly.GetExecutingAssembly();
			using var stream = assembly.GetManifestResourceStream("TransitManager.Mobile.appsettings.json");

			// --- AJOUTER CETTE VÉRIFICATION ---
			if (stream == null)
			{
				throw new InvalidOperationException("Impossible de trouver le fichier appsettings.json. Assurez-vous que son action de build est 'EmbeddedResource'.");
			}
			// --- FIN DE L'AJOUT ---

			var config = new ConfigurationBuilder()
						.AddJsonStream(stream) // On peut retirer le "!" maintenant
						.Build();

			builder.Configuration.AddConfiguration(config);
			string secret = builder.Configuration["InternalSecret"]!;
            // --- FIN DE LA MODIFICATION ---

            // Ajout du client Refit pour communiquer avec l'API
            builder.Services.AddRefitClient<ITransitApi>(refitSettings)
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri(baseApiUrl);
                    // --- AJOUTER CETTE LIGNE ---
                    c.DefaultRequestHeaders.Add("X-Internal-Secret", secret);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    // ATTENTION : Uniquement pour le développement.
                    // Permet d'ignorer les erreurs de certificat SSL auto-signé de Kestrel.
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                });

            // Injection des Vues et ViewModels
            builder.Services.AddSingleton<ClientsPage>();
            builder.Services.AddSingleton<ClientsViewModel>();

            builder.Services.AddTransient<ClientDetailPage>();
            builder.Services.AddTransient<ClientDetailViewModel>();

            builder.Services.AddTransient<AddEditClientPage>();
            builder.Services.AddTransient<AddEditClientViewModel>();

            builder.Services.AddSingleton<ColisPage>();
            builder.Services.AddSingleton<ColisViewModel>();

            builder.Services.AddTransient<ColisDetailPage>();
            builder.Services.AddTransient<ColisDetailViewModel>();

            builder.Services.AddTransient<AddEditColisPage>();
            builder.Services.AddTransient<AddEditColisViewModel>();

            builder.Services.AddSingleton<VehiculesPage>();
            builder.Services.AddSingleton<VehiculesViewModel>();

            builder.Services.AddTransient<VehiculeDetailPage>();
            builder.Services.AddTransient<VehiculeDetailViewModel>();

            builder.Services.AddTransient<AddEditVehiculePage>();
            builder.Services.AddTransient<AddEditVehiculeViewModel>();

            builder.Services.AddTransient<PaiementVehiculePage>();
            builder.Services.AddTransient<PaiementVehiculeViewModel>();

            builder.Services.AddTransient<AddEditPaiementPage>();
            builder.Services.AddTransient<AddEditPaiementViewModel>();

            builder.Services.AddTransient<PaiementColisPage>();
            builder.Services.AddTransient<PaiementColisViewModel>();

            builder.Services.AddTransient<EtatDesLieuxPage>();
            builder.Services.AddTransient<EtatDesLieuxViewModel>();

            builder.Services.AddTransient<EditEtatDesLieuxPage>();
            builder.Services.AddTransient<EditEtatDesLieuxViewModel>();

            builder.Services.AddTransient<InventairePage>();
            builder.Services.AddTransient<InventaireViewModel>();

            builder.Services.AddSingleton<ConteneurPage>();
            builder.Services.AddSingleton<ConteneurViewModel>();
            builder.Services.AddTransient<ConteneurDetailPage>();
            builder.Services.AddTransient<ConteneurDetailViewModel>();
            builder.Services.AddTransient<AddEditConteneurPage>();
            builder.Services.AddTransient<AddEditConteneurViewModel>();
            builder.Services.AddTransient<AddColisToConteneurPage>();
            builder.Services.AddTransient<AddColisToConteneurViewModel>();
            builder.Services.AddTransient<AddVehiculeToConteneurPage>();
            builder.Services.AddTransient<AddVehiculeToConteneurViewModel>();

            builder.Services.AddTransient<RemoveColisFromConteneurPage>();
            builder.Services.AddTransient<RemoveColisFromConteneurViewModel>();
            builder.Services.AddTransient<RemoveVehiculeFromConteneurPage>();
            builder.Services.AddTransient<RemoveVehiculeFromConteneurViewModel>();

            builder.Services.AddTransient<ClientSelectionPage>();
            builder.Services.AddTransient<ClientSelectionViewModel>();

            return builder.Build();
        }
    }
}
