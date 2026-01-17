using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities.Commerce
{
    public class Quote
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string Reference { get; set; } // e.g. DEV-2026-001

        public Guid? ClientId { get; set; } // Foreign Key (Nullable for Draft)

        [ForeignKey("ClientId")]
        public Client? Client { get; set; }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime DateValidity { get; set; } = DateTime.UtcNow.AddDays(30);

        public QuoteStatus Status { get; set; } = QuoteStatus.Draft;

        // Details
        [MaxLength(3000)]
        public string? Message { get; set; } // Message to user

        [MaxLength(500)]
        public string? PaymentTerms { get; set; } // e.g. "Comptant"

        [MaxLength(1000)]
        public string? FooterNote { get; set; } // Note de bas de page

        // Discount Configuration
        public decimal DiscountValue { get; set; }
        public DiscountType DiscountType { get; set; } = DiscountType.Percent;
        public DiscountBase DiscountBase { get; set; } = DiscountBase.BaseHT;
        public DiscountScope DiscountScope { get; set; } = DiscountScope.Total;

        // Public Access
        public Guid PublicToken { get; set; } = Guid.NewGuid();
        public DateTime? DateViewed { get; set; }
        public DateTime? DateSent { get; set; }
        public DateTime? DateAccepted { get; set; }
        public DateTime? DateRejected { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public string? SignatureData { get; set; } // JSON or Base64 of signature

        // Navigation
        public List<QuoteLine> Lines { get; set; } = new();

        // Totals (Computed or Cached)
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalHT { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTVA { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalTTC { get; set; }
    }
}
