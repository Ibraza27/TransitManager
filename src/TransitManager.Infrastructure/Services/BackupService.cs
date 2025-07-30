using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Npgsql;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class BackupService : IBackupService
    {
        private readonly TransitContext _context;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;
        private readonly string _backupDirectory;
        private System.Timers.Timer? _backupTimer;

        public BackupService(
            TransitContext context, 
            IConfiguration configuration,
            INotificationService notificationService)
        {
            _context = context;
            _configuration = configuration;
            _notificationService = notificationService;
            
            _backupDirectory = Path.Combine(
                _configuration["FileStorage:RootPath"] ?? "C:\\TransitManager\\Storage",
                _configuration["FileStorage:BackupsPath"] ?? "Backups"
            );

            Directory.CreateDirectory(_backupDirectory);
        }

        public async Task<string> CreateBackupAsync(string? customPath = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupName = $"TransitManager_Backup_{timestamp}";
                var backupPath = customPath ?? Path.Combine(_backupDirectory, backupName);
                
                Directory.CreateDirectory(backupPath);

                // 1. Backup de la base de données
                await BackupDatabaseAsync(Path.Combine(backupPath, "database.sql"));

                // 2. Backup des fichiers (documents, photos, etc.)
                await BackupFilesAsync(backupPath);

                // 3. Créer un fichier de métadonnées
                await CreateBackupMetadataAsync(backupPath);

                // 4. Compresser le tout
                var zipPath = $"{backupPath}.zip";
                ZipFile.CreateFromDirectory(backupPath, zipPath);

                // 5. Nettoyer le dossier temporaire
                Directory.Delete(backupPath, true);

                // Notification
                await _notificationService.NotifyAsync(
                    "Sauvegarde créée",
                    $"La sauvegarde a été créée avec succès : {Path.GetFileName(zipPath)}",
                    Core.Enums.TypeNotification.Succes
                );

                return zipPath;
            }
            catch (Exception ex)
            {
                await _notificationService.NotifyAsync(
                    "Erreur de sauvegarde",
                    $"Impossible de créer la sauvegarde : {ex.Message}",
                    Core.Enums.TypeNotification.Erreur,
                    Core.Enums.PrioriteNotification.Haute
                );
                throw;
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    throw new FileNotFoundException("Le fichier de sauvegarde n'existe pas.", backupPath);
                }

                // Vérifier l'intégrité avant la restauration
                if (!await VerifyBackupIntegrityAsync(backupPath))
                {
                    throw new InvalidOperationException("Le fichier de sauvegarde est corrompu.");
                }

                var tempPath = Path.Combine(Path.GetTempPath(), $"TransitRestore_{Guid.NewGuid()}");
                
                try
                {
                    // 1. Extraire l'archive
                    ZipFile.ExtractToDirectory(backupPath, tempPath);

                    // 2. Restaurer la base de données
                    var sqlPath = Path.Combine(tempPath, "database.sql");
                    if (File.Exists(sqlPath))
                    {
                        await RestoreDatabaseAsync(sqlPath);
                    }

                    // 3. Restaurer les fichiers
                    await RestoreFilesAsync(tempPath);

                    // Notification
                    await _notificationService.NotifyAsync(
                        "Restauration terminée",
                        "La restauration de la sauvegarde a été effectuée avec succès.",
                        Core.Enums.TypeNotification.Succes
                    );

                    return true;
                }
                finally
                {
                    // Nettoyer les fichiers temporaires
                    if (Directory.Exists(tempPath))
                    {
                        Directory.Delete(tempPath, true);
                    }
                }
            }
            catch (Exception ex)
            {
                await _notificationService.NotifyAsync(
                    "Erreur de restauration",
                    $"Impossible de restaurer la sauvegarde : {ex.Message}",
                    Core.Enums.TypeNotification.Erreur,
                    Core.Enums.PrioriteNotification.Haute
                );
                return false;
            }
        }

        public async Task<string[]> GetAvailableBackupsAsync()
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(_backupDirectory))
                    return Array.Empty<string>();

                return Directory.GetFiles(_backupDirectory, "TransitManager_Backup_*.zip")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .ToArray();
            });
        }

        public async Task<bool> DeleteOldBackupsAsync(int daysToKeep)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var backups = await GetAvailableBackupsAsync();
                var deletedCount = 0;

                foreach (var backup in backups)
                {
                    var fileInfo = new FileInfo(backup);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(backup);
                        deletedCount++;
                    }
                }

                if (deletedCount > 0)
                {
                    await _notificationService.NotifyAsync(
                        "Sauvegardes nettoyées",
                        $"{deletedCount} ancienne(s) sauvegarde(s) supprimée(s).",
                        Core.Enums.TypeNotification.Information
                    );
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ScheduleAutomaticBackupAsync(TimeSpan interval)
        {
            try
            {
                // Arrêter le timer existant
                _backupTimer?.Stop();
                _backupTimer?.Dispose();

                // Créer un nouveau timer
                _backupTimer = new System.Timers.Timer(interval.TotalMilliseconds);
                _backupTimer.Elapsed += async (sender, e) =>
                {
                    try
                    {
                        await CreateBackupAsync();
                        
                        // Nettoyer les anciennes sauvegardes
                        var retentionDays = _configuration.GetValue<int>("AppSettings:BackupRetentionDays", 30);
                        await DeleteOldBackupsAsync(retentionDays);
                    }
                    catch (Exception ex)
                    {
                        // Logger l'erreur sans interrompre le timer
                        Console.WriteLine($"Erreur lors de la sauvegarde automatique : {ex.Message}");
                    }
                };
                
                _backupTimer.Start();

                return await Task.FromResult(true);
            }
            catch
            {
                return false;
            }
        }

        public Task<BackupInfo> GetBackupInfoAsync(string backupPath) // On enlève "async"
        {
            return Task.Run(() => 
            {
                var fileInfo = new FileInfo(backupPath);
                var info = new BackupInfo
                {
                    FileName = fileInfo.Name,
                    FilePath = fileInfo.FullName,
                    Size = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime,
                    IsValid = false
                };

                try
                {
                    using var archive = ZipFile.OpenRead(backupPath);
                    var metadataEntry = archive.GetEntry("metadata.json");
                    
                    if (metadataEntry != null)
                    {
                        using var stream = metadataEntry.Open();
                        using var reader = new StreamReader(stream);
                        var json = reader.ReadToEnd();
                        var metadata = System.Text.Json.JsonSerializer.Deserialize<BackupMetadata>(json);
                        
                        if (metadata != null)
                        {
                            info.DatabaseVersion = metadata.DatabaseVersion;
                            info.AppVersion = metadata.AppVersion;
                            info.TotalFiles = metadata.TotalFiles;
                            info.TotalClients = metadata.TotalClients;
                            info.TotalColis = metadata.TotalColis;
                            info.TotalConteneurs = metadata.TotalConteneurs;
                            info.IsValid = true;
                        }
                    }
                }
                catch
                {
                    // Ignorer les erreurs de lecture
                }

                return info;
            });
        }

        public async Task<bool> VerifyBackupIntegrityAsync(string backupPath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var archive = ZipFile.OpenRead(backupPath);
                    
                    // Vérifier la présence des fichiers essentiels
                    var requiredFiles = new[] { "database.sql", "metadata.json" };
                    
                    foreach (var file in requiredFiles)
                    {
                        if (archive.GetEntry(file) == null)
                            return false;
                    }

                    // Vérifier que l'archive n'est pas corrompue
                    foreach (var entry in archive.Entries)
                    {
                        try
                        {
                            using var stream = entry.Open();
                            // Tenter de lire le premier octet
                            stream.ReadByte();
                        }
                        catch
                        {
                            return false;
                        }
                    }

                    return true;
                });
            }
            catch
            {
                return false;
            }
        }

        private async Task BackupDatabaseAsync(string outputPath)
        {
            var connectionString = _context.Database.GetConnectionString();
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            var backupCommand = $"pg_dump -h {builder.Host} -p {builder.Port} -U {builder.Username} -d {builder.Database} -f \"{outputPath}\"";

            // Pour Windows, utiliser pg_dump.exe
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -d {builder.Database}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment = { ["PGPASSWORD"] = builder.Password }
            };

            using var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                using var fileStream = new FileStream(outputPath, FileMode.Create);
                await process.StandardOutput.BaseStream.CopyToAsync(fileStream);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Erreur lors du backup de la base de données : {error}");
                }
            }
        }

        private async Task BackupFilesAsync(string backupPath)
        {
            var storageRoot = _configuration["FileStorage:RootPath"] ?? "C:\\TransitManager\\Storage";
            var foldersToBackup = new[]
            {
                "Clients",
                "Colis",
                "Containers",
                "Invoices"
            };

            foreach (var folder in foldersToBackup)
            {
                var sourcePath = Path.Combine(storageRoot, folder);
                if (Directory.Exists(sourcePath))
                {
                    var destPath = Path.Combine(backupPath, "Files", folder);
                    await CopyDirectoryAsync(sourcePath, destPath);
                }
            }
        }

        private async Task CopyDirectoryAsync(string sourceDir, string destDir)
        {
            await Task.Run(() =>
            {
                Directory.CreateDirectory(destDir);

                // Copier les fichiers
                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var destFile = Path.Combine(destDir, Path.GetFileName(file));
                    File.Copy(file, destFile, true);
                }

                // Copier les sous-dossiers
                foreach (var subDir in Directory.GetDirectories(sourceDir))
                {
                    var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                    CopyDirectoryAsync(subDir, destSubDir).Wait();
                }
            });
        }

        private async Task CreateBackupMetadataAsync(string backupPath)
        {
            var metadata = new BackupMetadata
            {
                BackupDate = DateTime.UtcNow,
                AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                DatabaseVersion = await GetDatabaseVersionAsync(),
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                TotalClients = await _context.Clients.CountAsync(),
                TotalColis = await _context.Colis.CountAsync(),
                TotalConteneurs = await _context.Conteneurs.CountAsync(),
                TotalFiles = Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories).Length
            };

            var json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(Path.Combine(backupPath, "metadata.json"), json);
        }

        private async Task<string> GetDatabaseVersionAsync()
        {
            // Récupérer la version depuis les migrations EF
            var migrations = await _context.Database.GetAppliedMigrationsAsync();
            return migrations.LastOrDefault() ?? "Initial";
        }

        private async Task RestoreDatabaseAsync(string sqlPath)
        {
            var connectionString = _context.Database.GetConnectionString();
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            // Créer une nouvelle base de données temporaire
            var tempDbName = $"{builder.Database}_restore_{DateTime.Now:yyyyMMddHHmmss}";
            
            // TODO: Implémenter la restauration complète de la base de données
            // Ceci nécessite des privilèges administrateur sur PostgreSQL
            
            await Task.CompletedTask;
        }

        private async Task RestoreFilesAsync(string backupPath)
        {
            var filesPath = Path.Combine(backupPath, "Files");
            if (!Directory.Exists(filesPath))
                return;

            var storageRoot = _configuration["FileStorage:RootPath"] ?? "C:\\TransitManager\\Storage";
            
            // Créer une sauvegarde de sécurité des fichiers actuels
            var currentBackup = Path.Combine(storageRoot, $"_backup_{DateTime.Now:yyyyMMddHHmmss}");
            if (Directory.Exists(storageRoot))
            {
                await CopyDirectoryAsync(storageRoot, currentBackup);
            }

            try
            {
                // Restaurer les fichiers
                await CopyDirectoryAsync(filesPath, storageRoot);
            }
            catch
            {
                // En cas d'erreur, restaurer la sauvegarde
                if (Directory.Exists(currentBackup))
                {
                    Directory.Delete(storageRoot, true);
                    Directory.Move(currentBackup, storageRoot);
                }
                throw;
            }
            finally
            {
                // Nettoyer la sauvegarde temporaire
                if (Directory.Exists(currentBackup))
                {
                    Directory.Delete(currentBackup, true);
                }
            }
        }
    }

}