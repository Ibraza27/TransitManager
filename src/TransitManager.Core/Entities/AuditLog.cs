using System;
using System.ComponentModel.DataAnnotations;

namespace TransitManager.Core.Entities
{
    /// <summary>
    /// Représente un log d'audit dans le système
    /// </summary>
    public class AuditLog : BaseEntity
    {
        /// <summary>
        /// ID de l'utilisateur qui a effectué l'action
        /// </summary>
        public Guid UtilisateurId { get; set; }

        /// <summary>
        /// Action effectuée (CREATE, UPDATE, DELETE, LOGIN, etc.)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Type d'entité concernée
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Entite { get; set; } = string.Empty;

        /// <summary>
        /// ID de l'entité concernée
        /// </summary>
        [StringLength(50)]
        public string? EntiteId { get; set; }

        /// <summary>
        /// Date et heure de l'action
        /// </summary>
        public DateTime DateAction { get; set; }

        /// <summary>
        /// Valeurs avant modification (JSON)
        /// </summary>
        public string? ValeurAvant { get; set; }

        /// <summary>
        /// Valeurs après modification (JSON)
        /// </summary>
        public string? ValeurApres { get; set; }

        /// <summary>
        /// Adresse IP de l'utilisateur
        /// </summary>
        [StringLength(45)]
        public string? AdresseIP { get; set; }

        /// <summary>
        /// User Agent du navigateur/application
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Commentaires additionnels
        /// </summary>
        public string? Commentaires { get; set; }

        // Navigation property
        /// <summary>
        /// Utilisateur qui a effectué l'action
        /// </summary>
        public virtual Utilisateur? Utilisateur { get; set; }

        /// <summary>
        /// Constructeur
        /// </summary>
        public AuditLog()
        {
            DateAction = DateTime.UtcNow;
        }
    }
}