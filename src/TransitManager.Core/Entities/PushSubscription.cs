using System;
using System.ComponentModel.DataAnnotations;

namespace TransitManager.Core.Entities
{
    /// <summary>
    /// Représente un abonnement Web Push d'un navigateur/appareil
    /// </summary>
    public class PushSubscription
    {
        public Guid Id { get; set; }

        /// <summary>
        /// L'utilisateur propriétaire de cet abonnement
        /// </summary>
        public Guid UtilisateurId { get; set; }
        public virtual Utilisateur? Utilisateur { get; set; }

        /// <summary>
        /// L'endpoint fourni par le navigateur (URL unique du push service)
        /// </summary>
        [Required]
        [StringLength(2048)]
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Clé P256DH (ECDH public key du client)
        /// </summary>
        [Required]
        [StringLength(512)]
        public string P256dh { get; set; } = string.Empty;

        /// <summary>
        /// Clé Auth (secret partagé)
        /// </summary>
        [Required]
        [StringLength(512)]
        public string Auth { get; set; } = string.Empty;

        /// <summary>
        /// User-Agent du navigateur (pour identification)
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public PushSubscription()
        {
            Id = Guid.NewGuid();
        }
    }
}
