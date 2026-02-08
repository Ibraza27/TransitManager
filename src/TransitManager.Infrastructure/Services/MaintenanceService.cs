using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace TransitManager.Infrastructure.Services
{
    public class MaintenanceService : BackgroundService
    {
        private readonly ILogger<MaintenanceService> _logger;
        private readonly Microsoft.Extensions.DependencyInjection.IServiceScopeFactory _scopeFactory;
        // Exécuter toutes les 24 heures
        private readonly TimeSpan _period = TimeSpan.FromHours(24);
        private readonly string _tempPath;

        public MaintenanceService(ILogger<MaintenanceService> logger, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _tempPath = Path.GetTempPath(); // Ou un dossier spécifique de l'app
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service de Maintenance démarré.");

            using var timer = new PeriodicTimer(_period);
            
            // Exécution immédiate ou attente ? Ici on attend le prochain tick (ou on pourrait lancer tout de suite)
            // On peut faire une première passe :
            await PerformMaintenanceAsync();

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                await PerformMaintenanceAsync();
            }
        }

        private async Task PerformMaintenanceAsync()
        {
            CleanupTemporaryFiles();
            
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var commerceService = scope.ServiceProvider.GetRequiredService<TransitManager.Core.Interfaces.ICommerceService>();
                    await commerceService.CheckOverdueInvoicesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la vérification des factures en retard.");
            }
        }

        private void CleanupTemporaryFiles()
        {
            try
            {
                _logger.LogInformation("Début du nettoyage des fichiers temporaires...");
                
                // Exemple : Nettoyer un dossier "Exports" s'il existe dans le dossier courant
                var exportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
                if (Directory.Exists(exportsDir))
                {
                    var files = Directory.GetFiles(exportsDir);
                    foreach (var file in files)
                    {
                        var fi = new FileInfo(file);
                        if (fi.CreationTimeUtc < DateTime.UtcNow.AddDays(-1))
                        {
                            try
                            {
                                File.Delete(file);
                                _logger.LogInformation($"Fichier supprimé : {fi.Name}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Impossible de supprimer {fi.Name} : {ex.Message}");
                            }
                        }
                    }
                }
                
                _logger.LogInformation("Nettoyage terminé.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la maintenance.");
            }
        }
    }
}
