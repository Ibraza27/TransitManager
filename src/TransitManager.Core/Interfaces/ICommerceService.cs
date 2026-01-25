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
    }
}
