using System;
using System.Collections.Generic;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs.Commerce
{
    public class QuoteDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; }
        public Guid? ClientId { get; set; }
        public string? ClientName { get; set; } // Flattened from Client.Nom
        public string? ClientFirstname { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientAddress { get; set; }
        public string? ClientEmail { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateValidity { get; set; }
        public QuoteStatus Status { get; set; }
        public string StatusName => Status.ToString();
        
        // History Dates
        public DateTime? DateSent { get; set; }
        public DateTime? DateAccepted { get; set; }
        public DateTime? DateRejected { get; set; }
        public DateTime? DateViewed { get; set; }
        
        public string? Message { get; set; }
        public string? PaymentTerms { get; set; }
        public string? FooterNote { get; set; }

        public decimal DiscountValue { get; set; }
        public DiscountType DiscountType { get; set; }
        public DiscountBase DiscountBase { get; set; }
        public DiscountScope DiscountScope { get; set; }

        public Guid PublicToken { get; set; }
        public string PublicUrl { get; set; } // Computed URL

        public string? RejectionReason { get; set; } // NEW

        public List<QuoteHistoryDto> History { get; set; } = new();
        public List<QuoteLineDto> Lines { get; set; } = new();

        public decimal TotalHT { get; set; }
        public decimal TotalTVA { get; set; }
        public decimal TotalTTC { get; set; }
    }

    public class QuoteLineDto
    {
        public Guid Id { get; set; }
        public Guid? ProductId { get; set; }
        public string Description { get; set; }
        public DateTime? Date { get; set; } // NEW
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal VATRate { get; set; }
        public decimal TotalHT { get; set; }
    }

    public class UpsertQuoteDto
    {
        public Guid? Id { get; set; }
        public string? Reference { get; set; } // For UI Preview only
        public Guid? ClientId { get; set; }
        public DateTime? DateCreated { get; set; } // For UI Preview only
        public DateTime DateValidity { get; set; }
        public string? Message { get; set; }
        public string? PaymentTerms { get; set; }
        public string? FooterNote { get; set; }
        
        public decimal DiscountValue { get; set; }
        public DiscountType DiscountType { get; set; }
        public DiscountBase DiscountBase { get; set; }
        public DiscountScope DiscountScope { get; set; }
        
        // Removed InternalNotes as it is not in Entity

        public List<QuoteLineDto> Lines { get; set; } = new();
    }

    public class QuoteHistoryDto
    {
        public DateTime Date { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public string User { get; set; }
    }
}
