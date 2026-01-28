using System;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.DTOs.Commerce;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Interfaces
{
    public interface ICommerceService
    {
        // Products
        Task<PagedResult<ProductDto>> GetProductsAsync(string? search, int page = 1, int pageSize = 50);
        Task<ProductDto> CreateProductAsync(ProductDto dto);
        Task<ProductDto> UpdateProductAsync(ProductDto dto);
        Task DeleteProductAsync(Guid id);

        // Quotes
        Task<PagedResult<QuoteDto>> GetQuotesAsync(string? search, Guid? clientId, string? status, int page = 1, int pageSize = 20);
        Task<QuoteDto?> GetQuoteByIdAsync(Guid id);
        Task<QuoteDto?> GetQuoteByTokenAsync(Guid token); // Public access
        Task<QuoteDto> CreateOrUpdateQuoteAsync(UpsertQuoteDto dto);
        Task<bool> UpdateQuoteStatusAsync(Guid id, QuoteStatus status, string? rejectionReason = null);
        Task<bool> DeleteQuoteAsync(Guid id);
        Task SendQuoteByEmailAsync(Guid id, string? subject = null, string? body = null, List<Guid>? attachmentIds = null);
        Task<byte[]> GenerateQuotePdfAsync(QuoteDto quote);

        // Invoices
        Task<PagedResult<InvoiceDto>> GetInvoicesAsync(string? search, Guid? clientId, string? status, int page = 1, int pageSize = 20);
        Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id);
        Task<InvoiceDto?> GetInvoiceByTokenAsync(Guid token); // Public access
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto);
        Task<InvoiceDto> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto dto);
        Task<InvoiceDto> DuplicateInvoiceAsync(Guid id);
        Task<bool> UpdateInvoiceStatusAsync(Guid id, InvoiceStatus status);
        Task<InvoiceDto> ConvertQuoteToInvoiceAsync(Guid quoteId);
        Task DeleteInvoiceAsync(Guid id);
        
        Task SendInvoiceByEmailAsync(Guid id, string? subject = null, string? body = null, List<string>? ccEmails = null);
        Task SendPaymentReminderAsync(Guid id, string? subject = null, string? body = null, List<Guid>? attachmentIds = null, List<string> ccEmails = null);
        Task<byte[]> GenerateInvoicePdfAsync(InvoiceDto invoice);
    }
}
