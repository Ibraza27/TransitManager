using System;
using System.ComponentModel.DataAnnotations; // Ajout nécessaire
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
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;
        
        public TypeNotification Type { get; set; }
        public PrioriteNotification Priorite { get; set; }
        
        public Guid? UtilisateurId { get; set; }
        public bool EstLue { get; set; }
        public DateTime? DateLecture { get; set; }
        
        // --- Navigation Web Classique ---
        [StringLength(500)]
        public string? ActionUrl { get; set; }
        
        [StringLength(100)]
        public string? ActionParametre { get; set; }

        // --- NOUVEAUX CHAMPS POUR DEEP LINKING (Mobile & App) ---
        // Permet de savoir programmatiquement vers quoi rediriger
        public Guid? RelatedEntityId { get; set; }
        
        [StringLength(50)]
        public string? RelatedEntityType { get; set; } // "Colis", "Vehicule", "Paiement"

        public virtual Utilisateur? Utilisateur { get; set; }
    }
}