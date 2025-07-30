using System;
using System.IO; // Nécessaire pour FileInfo, pour SizeFormatted

namespace TransitManager.Core.Entities
{
    public class BackupInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsValid { get; set; }
        public string? DatabaseVersion { get; set; }
        public string? AppVersion { get; set; }
        public int TotalFiles { get; set; }
        public int TotalClients { get; set; }
        public int TotalColis { get; set; }
        public int TotalConteneurs { get; set; }

        public string SizeFormatted
        {
            get
            {
                if (Size < 1024)
                    return $"{Size} B";
                else if (Size < 1024 * 1024)
                    return $"{Size / 1024.0:F2} KB";
                else if (Size < 1024 * 1024 * 1024)
                    return $"{Size / (1024.0 * 1024.0):F2} MB";
                else
                    return $"{Size / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
        }
    }

    public class BackupMetadata
    {
        public DateTime BackupDate { get; set; }
        public string AppVersion { get; set; } = string.Empty;
        public string DatabaseVersion { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int TotalClients { get; set; }
        public int TotalColis { get; set; }
        public int TotalConteneurs { get; set; }
        public int TotalFiles { get; set; }
    }
}