using System;

namespace TransitManager.Core.DTOs
{
    public class AuditLogDto
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; } // Can be null if system
        public string UserName { get; set; } = "System"; // Enhanced with Join
        public string Action { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? ValuesBefore { get; set; } // JSON
        public string? ValuesAfter { get; set; } // JSON
        // Optional: Pre-calculated diff summary
        public string Description { get; set; } = string.Empty;
    }
}
