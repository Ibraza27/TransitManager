using System;
using System.ComponentModel.DataAnnotations;

namespace TransitManager.Core.Entities
{
    public class ReceptionIssue : BaseEntity
    {
        public Guid ReceptionControlId { get; set; }

        public IssueType Type { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        // For Missing Items (Colis)
        public string? InventoryItemName { get; set; }
        public decimal? DeclaredValue { get; set; }

        // For Damage (Vehicule Schema)
        public double? X { get; set; }
        public double? Y { get; set; }
        
        public string? PhotoIds { get; set; } // Comma separated Document IDs if needed specific to issue

        public virtual ReceptionControl? ReceptionControl { get; set; }
    }

    public enum IssueType
    {
        MissingItem,
        Damage,
        Other
    }
}
