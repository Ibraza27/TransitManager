using System;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    public class NotificationEventArgs : EventArgs
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public TypeNotification Type { get; set; }
        public PrioriteNotification Priorite { get; set; }
    }

    public class Notification : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public TypeNotification Type { get; set; }
        public PrioriteNotification Priorite { get; set; }
        public Guid? UtilisateurId { get; set; }
        public bool EstLue { get; set; }
        public DateTime? DateLecture { get; set; }
        public string? ActionUrl { get; set; }
        public string? ActionParametre { get; set; }
        public virtual Utilisateur? Utilisateur { get; set; }
    }
}