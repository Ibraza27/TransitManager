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
        
        // Guest Client fields
        public string? GuestName { get; set; }
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        
        // Computed display name: prioritize Client, then GuestName, then GuestEmail
        public string DisplayName => !string.IsNullOrWhiteSpace(ClientName) 
            ? $"{ClientName} {ClientFirstname}".Trim() 
            : !string.IsNullOrWhiteSpace(GuestName) 
                ? GuestName 
                : GuestEmail ?? "Client inconnu";
        
        public string DisplayEmail => !string.IsNullOrWhiteSpace(ClientEmail) ? ClientEmail : (GuestEmail ?? "");
        public string DisplayPhone => !string.IsNullOrWhiteSpace(ClientPhone) ? ClientPhone : (GuestPhone ?? "");
        
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

        public string? RejectionReason { get; set; }

        // Links
        public Guid? InvoiceId { get; set; }
        public string? InvoiceReference { get; set; }

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
        // Round 7
        public QuoteLineType Type { get; set; }
        public int Position { get; set; }
    }

    public class UpsertQuoteDto
    {
        public Guid? Id { get; set; }
        public string? Reference { get; set; } // For UI Preview only
        public Guid? ClientId { get; set; }
        
        // Guest Client fields (used when ClientId is null)
        public string? GuestName { get; set; }
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        
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

        public decimal TotalHT { get; set; }
        public decimal TotalTVA { get; set; }
        public decimal TotalTTC { get; set; }
    }

    public class QuoteHistoryDto
    {
        public DateTime Date { get; set; }
        public string? Action { get; set; }
        public string? Details { get; set; }
        public string? User { get; set; }
    }
}
