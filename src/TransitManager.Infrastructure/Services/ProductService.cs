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
                .Where(p => p.Name.ToLower().Contains(term.ToLower()))
                .OrderBy(p => p.Name)
                .Take(20)
                .ToListAsync();
        }

        public async Task<int> ImportCsvAsync(string csvContent)
        {
            Console.WriteLine($"[CSV Import] Content Length: {csvContent?.Length ?? 0}");
            if (string.IsNullOrEmpty(csvContent)) return 0;

            var count = 0;
            var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine($"[CSV Import] Lines found: {lines.Length}");
            
            char delimiter = ',';
            
            var start = 0;
            if (lines.Length > 0)
            {
                 Console.WriteLine($"[CSV Import] Header line: {lines[0]}");
                 if (lines[0].Contains("Produit") || lines[0].Contains("Product")) 
                    start = 1;
            }

            for (int i = start; i < lines.Length; i++)
            {
                var line = lines[i];
                var parts = ParseCsvLine(line, delimiter);
                
                if (parts.Count < 3) 
                {
                    Console.WriteLine($"[CSV Import] Line {i} skipped (Parts < 3): {parts.Count} parts. Line: {line}");
                    continue; 
                }

                var name = parts[0].Trim(); 
                if (string.IsNullOrWhiteSpace(name)) 
                {
                     Console.WriteLine($"[CSV Import] Line {i} skipped (Empty Name)");
                     continue;
                }

                var unit = parts.Count > 1 ? parts[1].Trim() : "pce";
                if(string.IsNullOrWhiteSpace(unit)) unit = "pce";
                
                decimal priceHt = 0;
                if (parts.Count > 2) priceHt = ParseDecimal(parts[2]);

                decimal tva = 20; 
                if (parts.Count > 3) tva = ParseDecimal(parts[3]);
                
                Console.WriteLine($"[CSV Import] Importing: {name} | {priceHt} | {tva}");

                var existing = await _context.Products.FirstOrDefaultAsync(p => p.Name == name);
                if (existing != null)
                {
                    existing.UnitPrice = priceHt;
                    existing.Unit = unit;
                    existing.VATRate = tva;
                    _context.Entry(existing).State = EntityState.Modified;
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
                        Type = ProductType.Goods
                    };
                    _context.Products.Add(product);
                }
                count++;
            }
            await _context.SaveChangesAsync();
            Console.WriteLine($"[CSV Import] Saved. Total count: {count}");
            return count;
        }

        private decimal ParseDecimal(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 0;
            var clean = input.Replace("€", "").Replace("$", "").Replace("%", "").Trim();
            clean = clean.Replace("\u00A0", ""); // Non-breaking space
            
            // Priority: Invariant (Dot) then French (Comma)
            if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var resultInv))
            {
                return resultInv;
            }
            if (decimal.TryParse(clean, NumberStyles.Any, new CultureInfo("fr-FR"), out var resultFr))
            {
                return resultFr;
            }
            return 0;
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

        private List<string> ParseCsvLine(string line, char delimiter)
        {
            var result = new List<string>();
            var inQuotes = false;
            var current = new StringBuilder();
            
            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"') 
                {
                    // Handle escaped quotes ""
                    if (inQuotes && i + 1 < line.Length && line[i+1] == '"')
                    {
                         current.Append('"');
                         i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == delimiter && !inQuotes)
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
