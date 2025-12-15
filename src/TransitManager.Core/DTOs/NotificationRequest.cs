using System;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class NotificationRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Guid? UserId { get; set; } // Null = Broadcast Admin
        public CategorieNotification Categorie { get; set; }
        public string? ActionUrl { get; set; }
        public Guid? EntityId { get; set; }
        public string? EntityType { get; set; }
        public PrioriteNotification Priorite { get; set; } = PrioriteNotification.Normale;
    }
}
