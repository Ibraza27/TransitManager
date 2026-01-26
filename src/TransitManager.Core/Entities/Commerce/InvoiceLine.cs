using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities.Commerce
{
    public class InvoiceLine
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public Invoice Invoice { get; set; }

        public Guid? ProductId { get; set; } 
        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; }

        public DateTime? Date { get; set; } 

        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { get; set; } = 1;

        [MaxLength(20)]
        public string Unit { get; set; } = "pce";

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal VATRate { get; set; } = 0;

        // Computed
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalHT { get; set; }

        public QuoteLineType Type { get; set; } = QuoteLineType.Product;
        public int Position { get; set; }
    }
}
