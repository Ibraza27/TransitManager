using System;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class CreateNotificationDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public CategorieNotification Category { get; set; }
        public string? ActionUrl { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public PrioriteNotification Priority { get; set; } = PrioriteNotification.Normale;
    }
}
