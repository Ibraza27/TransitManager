using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitManager.Core.Entities.Commerce
{
    public class QuoteHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid QuoteId { get; set; }
        [ForeignKey("QuoteId")]
        public Quote Quote { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } // e.g. "Create", "Update", "Sent", "Viewed"

        [MaxLength(500)]
        public string? Details { get; set; } // e.g. "Status changed from Draft to Sent"

        public string? UserId { get; set; } // Optional: ID of user who performed the action
    }
}
