using System;
using System.Collections.Generic;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs.Commerce
{
    public class InvoiceDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; }
        
        public Guid? ClientId { get; set; }
        public string ClientName { get; set; }
        public string ClientEmail { get; set; }
        public string ClientPhone { get; set; }
        public string ClientAddress { get; set; }
        public string ClientFirstname { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DueDate { get; set; } 
        public DateTime? DatePaid { get; set; }
        public InvoiceStatus Status { get; set; }

        public string? Message { get; set; }
        public string? PaymentTerms { get; set; }
        public string? FooterNote { get; set; }

        public decimal TotalHT { get; set; }
        public decimal TotalTVA { get; set; }
        public decimal TotalTTC { get; set; }
        public decimal AmountPaid { get; set; }

        public Guid PublicToken { get; set; }

        public List<InvoiceLineDto> Lines { get; set; } = new();
        public List<InvoiceHistoryDto> History { get; set; } = new();
    }

    public class InvoiceLineDto
    {
        public Guid Id { get; set; }
        public Guid? ProductId { get; set; }
        public string Description { get; set; }
        public DateTime? Date { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal VATRate { get; set; }
        public decimal TotalHT { get; set; }
        public QuoteLineType Type { get; set; }
        public int Position { get; set; }
    }

    public class InvoiceHistoryDto
    {
        public DateTime Date { get; set; }
        public string Action { get; set; }
        public string UserName { get; set; }
        public string Details { get; set; }
    }
}
