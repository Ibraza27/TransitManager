using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitManager.Core.Entities
{
    public class PushSubscription : BaseEntity
    {
        [Required]
        public string Endpoint { get; set; } = null!; // Unique endpoint from browser
        public string? P256dh { get; set; } // Key for encryption
        public string? Auth { get; set; } // Key for auth

        public Guid? UtilisateurId { get; set; }
        [ForeignKey("UtilisateurId")]
        public virtual Utilisateur? Utilisateur { get; set; }

        public string? UserAgent { get; set; } // To identify device type if needed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
