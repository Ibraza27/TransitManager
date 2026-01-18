using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransitManager.Core.Entities.Commerce;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly TransitContext _context;

        public ProductService(TransitContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.OrderBy(p => p.Name).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product> UpdateAsync(Product product)
        {
            _context.Entry(product).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task DeleteManyAsync(IEnumerable<Guid> ids)
        {
            var products = await _context.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
            if (products.Any())
            {
                _context.Products.RemoveRange(products);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Product>> SearchAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return new List<Product>();
            
            return await _context.Products
                .Where(p => p.Name.Contains(term))
                .OrderBy(p => p.Name)
                .Take(20)
                .ToListAsync();
        }

        public async Task<int> ImportCsvAsync(string csvContent)
        {
            var count = 0;
            var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Expected Header: "Produit,Unité,Prix HT (EUR),TVA,Prix TTC (EUR)"
            // Skipping header if present
            var start = 0;
            if (lines.Length > 0 && lines[0].Contains("Produit")) start = 1;

            for (int i = start; i < lines.Length; i++)
            {
                var line = lines[i];
                var parts = ParseCsvLine(line);
                if (parts.Count < 4) continue; // Minimum required

                var name = parts[0];
                var unit = parts[1];
                if(string.IsNullOrWhiteSpace(unit)) unit = "pce";
                
                // Parse Price HT
                if (!decimal.TryParse(parts[2].Replace("€","").Replace(" ",""), NumberStyles.Any, CultureInfo.InvariantCulture, out var priceHt))
                {
                     // Try French format
                     decimal.TryParse(parts[2].Replace("€","").Replace(" ",""), NumberStyles.Any, new CultureInfo("fr-FR"), out priceHt);
                }

                // Parse TVA (e.g. "20 %" or "0.2")
                var tvaStr = parts[3].Replace("%", "").Replace(" ", "");
                if (!decimal.TryParse(tvaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var tva))
                {
                     decimal.TryParse(tvaStr, NumberStyles.Any, new CultureInfo("fr-FR"), out tva);
                }
                
                // Check if exists
                var existing = await _context.Products.FirstOrDefaultAsync(p => p.Name == name);
                if (existing != null)
                {
                    existing.UnitPrice = priceHt;
                    existing.Unit = unit;
                    existing.VATRate = tva;
                    // Type ? Default to Goods for now unless guessed
                }
                else
                {
                    var product = new Product
                    {
                        Id = Guid.NewGuid(),
                        Name = name,
                        Unit = unit,
                        UnitPrice = priceHt,
                        VATRate = tva,
                        Type = ProductType.Goods // Default
                    };
                    _context.Products.Add(product);
                }
                count++;
            }
            await _context.SaveChangesAsync();
            return count;
        }

        public Task<byte[]> ExportCsvAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Produit,Unité,Prix HT (EUR),TVA,Prix TTC (EUR)");
            
            var products = _context.Products.OrderBy(p => p.Name).AsNoTracking();
            foreach (var p in products)
            {
                var priceTtc = p.UnitPrice * (1 + p.VATRate / 100m);
                sb.AppendLine($"{EscapeCsv(p.Name)},{p.Unit},{p.UnitPrice.ToString("F2", CultureInfo.InvariantCulture)},{p.VATRate.ToString("F2", CultureInfo.InvariantCulture)},{priceTtc.ToString("F2", CultureInfo.InvariantCulture)}");
            }
            
            return Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var inQuotes = false;
            var current = new StringBuilder();
            
            foreach (var c in line)
            {
                if (c == '"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result;
        }

        private string EscapeCsv(string field)
        {
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
