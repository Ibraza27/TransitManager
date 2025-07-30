using System;
using System.Threading.Tasks;
using TransitManager.Core.Entities; // Cette ligne est cruciale !

namespace TransitManager.Core.Interfaces
{
    public interface IBackupService
    {
        Task<string> CreateBackupAsync(string? customPath = null);
        Task<bool> RestoreBackupAsync(string backupPath);
        Task<string[]> GetAvailableBackupsAsync();
        Task<bool> DeleteOldBackupsAsync(int daysToKeep);
        Task<bool> ScheduleAutomaticBackupAsync(TimeSpan interval);
        Task<BackupInfo> GetBackupInfoAsync(string backupPath); // L'interface voit maintenant BackupInfo depuis Core.Entities
        Task<bool> VerifyBackupIntegrityAsync(string backupPath);
    }
}