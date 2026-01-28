using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs.Commerce
{
    public class CreateInvoiceDto
    {
        public Guid? ClientId { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);
        public string? Message { get; set; }
        public string? PaymentTerms { get; set; }
        public string? FooterNote { get; set; }
        public List<UpdateInvoiceLineDto> Lines { get; set; } = new();
    }

    public class UpdateInvoiceDto
    {
        public Guid Id { get; set; }
        public Guid? ClientId { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DueDate { get; set; }
        
        public string? Message { get; set; }
        public string? PaymentTerms { get; set; }
        public string? FooterNote { get; set; }

        public decimal DiscountValue { get; set; }
        public DiscountType DiscountType { get; set; }
        public DiscountBase DiscountBase { get; set; }
        public DiscountScope DiscountScope { get; set; }

        public InvoiceStatus Status { get; set; }
        
        public List<UpdateInvoiceLineDto> Lines { get; set; } = new();
    }

    public class UpdateInvoiceLineDto
    {
        public Guid? Id { get; set; } // Null if new
        public Guid? ProductId { get; set; }
        public string Description { get; set; }
        public DateTime? Date { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal VATRate { get; set; }
        public QuoteLineType Type { get; set; }
        public int Position { get; set; }
    }
}
