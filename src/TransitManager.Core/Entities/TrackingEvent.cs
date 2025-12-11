using System;
using System.ComponentModel.DataAnnotations;

namespace TransitManager.Core.Entities
{
    public class TrackingEvent : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Description { get; set; } = string.Empty; // Message technique ou interne

        [StringLength(200)]
        public string? DescriptionPublique { get; set; } // Message affiché au client (ex: "Votre colis est en route")

        public DateTime EventDate { get; set; }
        
        [StringLength(100)]
        public string? Location { get; set; } // Lieu de l'événement (ex: "Port de Marseille")

        // Liaisons (Un événement peut concerner plusieurs entités si elles sont liées, ex: Colis dans Conteneur)
        public Guid? ColisId { get; set; }
        public virtual Colis? Colis { get; set; }

        public Guid? VehiculeId { get; set; }
        public virtual Vehicule? Vehicule { get; set; }

        public Guid? ConteneurId { get; set; }
        public virtual Conteneur? Conteneur { get; set; }

        // Statut à ce moment précis (snapshot)
        [StringLength(50)]
        public string? Statut { get; set; }

        public bool IsAutomatic { get; set; } = true; // Généré par le système ou manuel

        public TrackingEvent()
        {
            EventDate = DateTime.UtcNow;
        }
    }
}