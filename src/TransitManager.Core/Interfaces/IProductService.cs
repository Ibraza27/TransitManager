using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities.Commerce;

namespace TransitManager.Core.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(Guid id);
        Task<Product> CreateAsync(Product product);
        Task<Product> UpdateAsync(Product product);
        Task DeleteAsync(Guid id);
        Task DeleteManyAsync(IEnumerable<Guid> ids);
        
        // Import/Export
        Task<int> ImportCsvAsync(string csvContent);
        Task<byte[]> ExportCsvAsync();
        
        Task<IEnumerable<Product>> SearchAsync(string term);
    }
}
