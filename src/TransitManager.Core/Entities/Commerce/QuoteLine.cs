using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransitManager.Core.Entities.Commerce
{
    public class QuoteLine
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid QuoteId { get; set; }
        [ForeignKey("QuoteId")]
        public Quote Quote { get; set; }

        public Guid? ProductId { get; set; } // Optional link to catalog
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; }

        public DateTime? Date { get; set; } // Optional service date

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; } = 1;

        [MaxLength(20)]
        public string Unit { get; set; } = "pce";

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VATRate { get; set; } = 0; // 0, 10, 20

        // Computed
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalHT { get; set; }
    }
}
