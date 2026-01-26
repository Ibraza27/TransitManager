using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitManager.Core.Entities.Commerce
{
    public class InvoiceHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public Invoice Invoice { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } // "Créé", "Envoyé", "Modifié"

        public Guid? UserId { get; set; }
        
        [MaxLength(50)]
        public string? UserName { get; set; }

        [MaxLength(500)]
        public string? Details { get; set; }
    }
}
