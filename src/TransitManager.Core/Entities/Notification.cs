using System;
using System.ComponentModel.DataAnnotations;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    public class NotificationEventArgs : EventArgs
    {
        public Notification Notification { get; set; } = new();
    }

    public class Notification : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;
        
        // Visuel
        public string Icone { get; set; } = "bi-info-circle"; // Classe CSS Bootstrap Icons par défaut
        public string Couleur { get; set; } = "text-primary"; // Classe CSS couleur
        
        public TypeNotification Type { get; set; }
        public PrioriteNotification Priorite { get; set; }
        public CategorieNotification Categorie { get; set; } // NOUVEAU
        
        // Destinataire
        public Guid? UtilisateurId { get; set; } // Si null = Broadcast (ex: tous les admins)
        public virtual Utilisateur? Utilisateur { get; set; }
        
        // État
        public bool EstLue { get; set; }
        public DateTime? DateLecture { get; set; }
        
        // Deep Linking (Navigation)
        [StringLength(500)]
        public string? ActionUrl { get; set; } // URL relative (ex: "/colis/edit/...")
        
        public Guid? RelatedEntityId { get; set; }
        [StringLength(50)]
        public string? RelatedEntityType { get; set; } // "Colis", "Vehicule", etc.

        public Notification()
        {
            DateCreation = DateTime.UtcNow;
            EstLue = false;
        }
    }
}