using System;
using System.ComponentModel.DataAnnotations;

namespace TransitManager.Core.Entities
{
    public class Message : BaseEntity
    {
        [Required]
        public string Contenu { get; set; } = string.Empty;

        public DateTime DateEnvoi { get; set; }

        // Expéditeur
        public Guid ExpediteurId { get; set; }
        public virtual Utilisateur Expediteur { get; set; } = null!;

        // Contexte (Un seul des deux sera rempli généralement)
        public Guid? ColisId { get; set; }
        public virtual Colis? Colis { get; set; }

        public Guid? VehiculeId { get; set; }
        public virtual Vehicule? Vehicule { get; set; }

        // Options
        public bool IsInternal { get; set; } // Si True, invisible pour le client
        public bool IsRead { get; set; }
        public DateTime? DateLecture { get; set; }

        // Pièce jointe optionnelle (Document existant ou nouveau)
        public Guid? DocumentId { get; set; }
        public virtual Document? PieceJointe { get; set; }

        public Message()
        {
            DateEnvoi = DateTime.UtcNow;
            IsInternal = false;
            IsRead = false;
        }
    }
}