using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities.Commerce
{
    public class Invoice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Reference { get; set; } // e.g. FAC-2026-001

        public Guid? ClientId { get; set; }
        [ForeignKey("ClientId")]
        public Client? Client { get; set; }
        
        // Guest Client (not stored in Client table)
        [MaxLength(200)]
        public string? GuestName { get; set; }      // Optional name
        
        [MaxLength(200)]
        public string? GuestEmail { get; set; }     // Required if no ClientId
        
        [MaxLength(50)]
        public string? GuestPhone { get; set; }     // Optional
        
        // Link to original Quote if converted
        public Guid? QuoteId { get; set; }
        // We generally don't enforce FK here to avoid cascade issues, or make it optional.
        
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30); // Échéance
        public DateTime? DatePaid { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        // Details
        [MaxLength(3000)]
        public string? Message { get; set; } 

        [MaxLength(500)]
        public string? PaymentTerms { get; set; } 

        [MaxLength(1000)]
        public string? FooterNote { get; set; } 

        // Discount
        public decimal DiscountValue { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.Percent;
        public DiscountBase DiscountBase { get; set; } = DiscountBase.BaseHT;
        public DiscountScope DiscountScope { get; set; } = DiscountScope.Total;

        // Public Access
        public Guid PublicToken { get; set; } = Guid.NewGuid();
        public DateTime? DateViewed { get; set; }
        public DateTime? DateSent { get; set; }
        public DateTime? LastReminderSent { get; set; }
        public int ReminderCount { get; set; } = 0;

        // Navigation
        public List<InvoiceLine> Lines { get; set; } = new();
        // We can reuse QuoteHistory or create InvoiceHistory. Let's reuse basic approach or create new.
        // For simplicity and separation, let's assume we might want InvoiceHistory later, but for now strict copy of Quote logic implies we want History.
        // But AuditLog might be enough? Quote had "History" collection. Let's create InvoiceHistory to match Zervant expectation.
        public List<InvoiceHistory> History { get; set; } = new();

        // Totals
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalHT { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTVA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTTC { get; set; }
        
        // Payments
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; } // Acomptes ou paiements partiels
    }
}
