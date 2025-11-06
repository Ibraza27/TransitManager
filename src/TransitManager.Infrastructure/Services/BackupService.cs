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
                "Backups"
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
                await BackupDatabaseAsync(Path.Combine(backupPath, "database.sql"));
                await CreateBackupMetadataAsync(backupPath);
                var zipPath = $"{backupPath}.zip";
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(backupPath, zipPath);
                Directory.Delete(backupPath, true);
                await _notificationService.NotifyAsync("Sauvegarde créée", $"La sauvegarde a été créée avec succès : {Path.GetFileName(zipPath)}", Core.Enums.TypeNotification.Succes);
                return zipPath;
            }
            catch (Exception ex)
            {
                await _notificationService.NotifyAsync("Erreur de sauvegarde", $"Impossible de créer la sauvegarde : {ex.Message}", Core.Enums.TypeNotification.Erreur, Core.Enums.PrioriteNotification.Haute);
                throw;
            }
        }

        public async Task<bool> RestoreBackupAsync(string backupPath)
        {
            await _notificationService.NotifyAsync("Information", "La fonctionnalité de restauration n'est pas encore implémentée.");
            return await Task.FromResult(false);
        }

        public Task<string[]> GetAvailableBackupsAsync()
        {
            if (!Directory.Exists(_backupDirectory))
                return Task.FromResult(Array.Empty<string>());
            var files = Directory.GetFiles(_backupDirectory, "TransitManager_Backup_*.zip")
                .OrderByDescending(f => new FileInfo(f).CreationTime)
                .ToArray();
            return Task.FromResult(files);
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
                    await _notificationService.NotifyAsync("Sauvegardes nettoyées", $"{deletedCount} ancienne(s) sauvegarde(s) supprimée(s).");
                }
                return true;
            }
            catch { return false; }
        }

        public Task<bool> ScheduleAutomaticBackupAsync(TimeSpan interval)
        {
            _backupTimer?.Dispose();
            _backupTimer = new System.Timers.Timer(interval.TotalMilliseconds);
            _backupTimer.Elapsed += async (s, e) => {
                await CreateBackupAsync();
                await DeleteOldBackupsAsync(_configuration.GetValue<int>("AppSettings:BackupRetentionDays", 30));
            };
            _backupTimer.Start();
            return Task.FromResult(true);
        }

        public Task<BackupInfo> GetBackupInfoAsync(string backupPath)
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
                    var metadata = System.Text.Json.JsonSerializer.Deserialize<BackupMetadata>(reader.ReadToEnd());
                    if (metadata != null)
                    {
                        info.AppVersion = metadata.AppVersion;
                        info.TotalClients = metadata.TotalClients;
                        info.IsValid = true;
                    }
                }
            }
            catch { }
            return Task.FromResult(info);
        }

        public Task<bool> VerifyBackupIntegrityAsync(string backupPath)
        {
            try
            {
                using var archive = ZipFile.OpenRead(backupPath);
                return Task.FromResult(archive.GetEntry("database.sql") != null && archive.GetEntry("metadata.json") != null);
            }
            catch { return Task.FromResult(false); }
        }

        private async Task BackupDatabaseAsync(string outputPath)
        {
            var connectionString = _context.Database.GetConnectionString();
            var builder = new NpgsqlConnectionStringBuilder(connectionString);
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
                await using var fileStream = new FileStream(outputPath, FileMode.Create);
                await process.StandardOutput.BaseStream.CopyToAsync(fileStream);
                await process.WaitForExitAsync();
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Erreur pg_dump : {error}");
                }
            }
        }

        private async Task CreateBackupMetadataAsync(string backupPath)
        {
            var metadata = new BackupMetadata
            {
                BackupDate = DateTime.UtcNow,
                AppVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0",
                DatabaseVersion = (await _context.Database.GetAppliedMigrationsAsync()).LastOrDefault() ?? "Initial",
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                TotalClients = await _context.Clients.CountAsync(),
                TotalColis = await _context.Colis.CountAsync(),
                TotalConteneurs = await _context.Conteneurs.CountAsync(),
                TotalFiles = Directory.Exists(Path.Combine(backupPath, "Files")) ? Directory.GetFiles(Path.Combine(backupPath, "Files"), "*", SearchOption.AllDirectories).Length : 0
            };
            var json = System.Text.Json.JsonSerializer.Serialize(metadata, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(Path.Combine(backupPath, "metadata.json"), json);
        }
    }
}
