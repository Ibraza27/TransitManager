using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitManager.Core.Entities
{
    public class ReceptionControl : BaseEntity
    {
        public Guid? ColisId { get; set; }
        public Guid? VehiculeId { get; set; }
        public Guid ClientId { get; set; }

        public ReceptionStatus Status { get; set; }

        public bool IsValidated { get; set; } = false;
        public DateTime CreationDate { get; set; } = DateTime.UtcNow;

        // Ratings (0-10)
        [Range(0, 10)]
        public int RateService { get; set; }
        [Range(0, 10)]
        public int RateCondition { get; set; }
        [Range(0, 10)]
        public int RateCommunication { get; set; }
        [Range(0, 10)]
        public int RateTimeframe { get; set; } // Ponctualité/Délai
        [Range(0, 10)]
        public int RateRecommendation { get; set; } // Satisfaction Générale/Recommandation

        public string? CommentService { get; set; }
        public string? CommentCondition { get; set; }
        public string? CommentCommunication { get; set; }
        public string? CommentTimeframe { get; set; }
        public string? CommentRecommendation { get; set; }
        
        [StringLength(2000)]
        public string? GlobalComment { get; set; }

        public string? SavSchemaPointsJson { get; set; }
        public string? SavSchemaStrokesJson { get; set; }

        // Navigation
        public virtual List<ReceptionIssue> Issues { get; set; } = new();
        public virtual Colis? Colis { get; set; }
        public virtual Vehicule? Vehicule { get; set; }
        public virtual Client? Client { get; set; }
    }

    public enum ReceptionStatus
    {
        ReceivedFull,       // Tout reçu / Conforme
        ReceivedPartial,    // Manquant
        ReceivedDamaged     // Endommagé (Véhicule ou Colis)
    }
}
