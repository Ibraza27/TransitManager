using System;
using System.Collections.Generic;
using TransitManager.Core.DTOs.Commerce;
using TransitManager.Core.Enums;

namespace TransitManager.Web.ViewModels
{
    public class InvoiceViewModel
    {
        public Guid? Id { get; set; }
        public string? Reference { get; set; }
        public Guid ClientId { get; set; }
        public string? GuestName { get; set; }
        public string? GuestEmail { get; set; }
        public string? GuestPhone { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.Today;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(30);
        public DateTime? DatePaid { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
        public string? Message { get; set; }
        public string? FooterNote { get; set; }
        public string? PaymentTerms { get; set; }
        public decimal DiscountValue { get; set; }
        public DiscountType DiscountType { get; set; }
        public DiscountBase DiscountBase { get; set; }
        public DiscountScope DiscountScope { get; set; }
        public decimal AmountPaid { get; set; }
        public Guid PublicToken { get; set; }
        public Guid? QuoteId { get; set; }
        public string? QuoteReference { get; set; }
        public List<InvoiceLineVM> Lines { get; set; } = new();
    }

    public class InvoiceLineVM
    {
        public Guid? Id { get; set; }
        public Guid? ProductId { get; set; }
        public string Description { get; set; } = "";
        public DateTime? Date { get; set; }
        public decimal Quantity { get; set; } = 1;
        public string Unit { get; set; } = "pce";
        public decimal UnitPrice { get; set; }
        public decimal VATRate { get; set; }
        public decimal TotalHT { get; set; }
        public QuoteLineType Type { get; set; } = QuoteLineType.Product;
        public int Position { get; set; }
    }
}
