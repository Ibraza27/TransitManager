using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransitManager.Core.DTOs;
using TransitManager.Core.DTOs.Commerce;
using TransitManager.Core.Entities.Commerce;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class CommerceService : ICommerceService
    {
        private readonly TransitContext _context;
        private readonly IEmailService _emailService;
        private readonly IExportService _exportService;

        public CommerceService(TransitContext context, IEmailService emailService, IExportService exportService)
        {
            _context = context;
            _emailService = emailService;
            _exportService = exportService;
        }

        // --- Products ---

        public async Task<PagedResult<ProductDto>> GetProductsAsync(string? search, int page = 1, int pageSize = 50)
        {
            var query = _context.Products.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    UnitPrice = p.UnitPrice,
                    Unit = p.Unit,
                    VATRate = p.VATRate
                }).ToListAsync();

            return new PagedResult<ProductDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<ProductDto> CreateProductAsync(ProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                UnitPrice = dto.UnitPrice,
                Unit = dto.Unit,
                VATRate = dto.VATRate
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            dto.Id = product.Id;
            return dto;
        }

        public async Task<ProductDto> UpdateProductAsync(ProductDto dto)
        {
            var product = await _context.Products.FindAsync(dto.Id);
            if (product == null) throw new Exception("Product not found");

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.UnitPrice = dto.UnitPrice;
            product.Unit = dto.Unit;
            product.VATRate = dto.VATRate;

            await _context.SaveChangesAsync();
            return dto;
        }

        public async Task DeleteProductAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        // --- Quotes ---

        public async Task<PagedResult<QuoteDto>> GetQuotesAsync(string? search, Guid? clientId, string? status, int page = 1, int pageSize = 20)
        {
            var query = _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.Lines)
                .Include(q => q.History)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(q => q.Reference.Contains(search) || q.Client.Nom.Contains(search));
            }
            if (clientId.HasValue)
            {
                query = query.Where(q => q.ClientId == clientId.Value);
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<QuoteStatus>(status, true, out var statusEnum))
            {
                query = query.Where(q => q.Status == statusEnum);
            }

            var total = await query.CountAsync();
            // Execute query and map in memory to avoid EF Translation issues with MapToDto
            var entities = await query.OrderByDescending(q => q.DateCreated)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var items = entities.Select(q => MapToDto(q)).ToList();

            return new PagedResult<QuoteDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<QuoteDto?> GetQuoteByIdAsync(Guid id)
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.Lines)
                .Include(q => q.History)
                .FirstOrDefaultAsync(q => q.Id == id);
            return quote == null ? null : MapToDto(quote);
        }

        public async Task<QuoteDto?> GetQuoteByTokenAsync(Guid token)
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.Lines)
                .Include(q => q.History)
                .FirstOrDefaultAsync(q => q.PublicToken == token);
            
            if (quote != null && quote.Status == QuoteStatus.Sent)
            {
                quote.Status = QuoteStatus.Viewed;
                quote.DateViewed = DateTime.UtcNow;
                
                _context.QuoteHistories.Add(new QuoteHistory 
                { 
                    QuoteId = quote.Id, 
                    Date = DateTime.UtcNow, 
                    Action = "Consulté en ligne", 
                    Details = "Devis visionné via le lien public" 
                });
                
                await _context.SaveChangesAsync();
            }

            return quote == null ? null : MapToDto(quote);
        }

        public async Task<QuoteDto> CreateOrUpdateQuoteAsync(UpsertQuoteDto dto)
        {
            try { System.IO.File.AppendAllText("commerce_debug.txt", $"[{DateTime.Now}] INPUT: Id={dto.Id}, ClientId={dto.ClientId}, LinesCount={dto.Lines?.Count} \n"); } catch {}
            
            Quote quote;
            if (dto.Id.HasValue)
            {
                quote = await _context.Quotes.Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == dto.Id);
                if (quote == null) throw new Exception("Quote not found");
                
                // Update basic fields
                quote.ClientId = dto.ClientId;
                quote.DateValidity = dto.DateValidity;
                quote.Message = dto.Message;
                quote.PaymentTerms = dto.PaymentTerms;
                quote.FooterNote = dto.FooterNote;
                quote.DiscountValue = dto.DiscountValue;
                quote.DiscountType = dto.DiscountType;
                quote.DiscountBase = dto.DiscountBase;
                quote.DiscountScope = dto.DiscountScope;
                
                // Clear lines to rebuild (simple strategy for now)
                _context.QuoteLines.RemoveRange(quote.Lines);
            }
            else
            {
                quote = new Quote
                {
                    Reference = await GenerateQuoteReferenceAsync(),
                    ClientId = dto.ClientId,
                    DateCreated = DateTime.UtcNow,
                    DateValidity = dto.DateValidity,
                    Message = dto.Message,
                    PaymentTerms = dto.PaymentTerms,
                    FooterNote = dto.FooterNote,
                    DiscountValue = dto.DiscountValue,
                    DiscountType = dto.DiscountType,
                    DiscountBase = dto.DiscountBase,
                    DiscountScope = dto.DiscountScope,
                    Status = QuoteStatus.Draft
                };
                _context.Quotes.Add(quote);
                
                // History: Create
                _context.QuoteHistories.Add(new QuoteHistory 
                { 
                    QuoteId = quote.Id, 
                    Date = DateTime.UtcNow, 
                    Action = "Création", 
                    Details = "Devis créé" 
                });
            }

            // Add Lines
            var newLines = new List<QuoteLine>();
            decimal grossHT = 0;
            decimal grossTVA = 0;

            foreach (var lineDto in dto.Lines)
            {
                var lineTotalHT = lineDto.Quantity * lineDto.UnitPrice;
                var lineTVA = lineTotalHT * (lineDto.VATRate / 100m);
                
                grossHT += lineTotalHT;
                grossTVA += lineTVA;

                newLines.Add(new QuoteLine
                {
                    ProductId = lineDto.ProductId,
                    Description = lineDto.Description,
                    Date = lineDto.Date,
                    Quantity = lineDto.Quantity,
                    Unit = lineDto.Unit,
                    UnitPrice = lineDto.UnitPrice,
                    VATRate = lineDto.VATRate,
                    TotalHT = lineTotalHT
                });
            }

            quote.Lines = newLines;

            // --- Calculation Logic ---
            // Simplified logic: We apply discount on HT for now. 
            // If user selected BaseTTC, we reverse calc, but let's stick to standard B2B logic primarily.
            
            decimal discountAmount = 0;
            
            if (quote.DiscountType == DiscountType.Percent)
            {
                // % on Gross HT
                discountAmount = grossHT * (quote.DiscountValue / 100m);
            }
            else 
            {
                // Fixed Amount
                discountAmount = quote.DiscountValue;
            }

            // Safety check
            if (discountAmount > grossHT) discountAmount = grossHT;

            quote.TotalHT = grossHT - discountAmount;
            
            // Prorate TVA reduction: NewTVA = OldTVA * (1 - DiscountRatio)
            // DiscountRatio = DiscountAmount / GrossHT
            decimal discountRatio = grossHT == 0 ? 0 : (discountAmount / grossHT);
            quote.TotalTVA = grossTVA * (1 - discountRatio);

            quote.TotalTTC = quote.TotalHT + quote.TotalTVA;

            try 
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // DETECT PHANTOM EXCEPTION ON INSERT
                var exists = await _context.Quotes.AnyAsync(q => q.Id == quote.Id);
                if (!exists) throw;

                // Ensure context is clean
                _context.Entry(quote).State = EntityState.Detached;

                // RECOVERY: Check if data was lost (e.g. Phantom Insert of empty row)
                var savedQuote = await _context.Quotes.Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == quote.Id);
                
                if (savedQuote != null)
                {
                    bool needSave = false;
                    
                    // 1. Recover Header Fields (Client, Totals, Strings) if missing or default
                    // We assume 'quote' has the correct intent. We force update the saved instance.
                    savedQuote.ClientId = quote.ClientId;
                    savedQuote.DateValidity = quote.DateValidity;
                    savedQuote.Message = quote.Message;
                    savedQuote.PaymentTerms = quote.PaymentTerms;
                    savedQuote.FooterNote = quote.FooterNote;
                    savedQuote.DiscountValue = quote.DiscountValue;
                    savedQuote.DiscountType = quote.DiscountType;
                    savedQuote.DiscountBase = quote.DiscountBase;
                    savedQuote.DiscountScope = quote.DiscountScope;
                    savedQuote.TotalHT = quote.TotalHT;
                    savedQuote.TotalTVA = quote.TotalTVA;
                    savedQuote.TotalTTC = quote.TotalTTC;
                    savedQuote.Reference = quote.Reference; // Ensure ref matches
                    savedQuote.DateCreated = quote.DateCreated;
                    
                    needSave = true;

                    // 2. Recover Lines if missing
                    if ((savedQuote.Lines == null || savedQuote.Lines.Count == 0) && dto.Lines != null && dto.Lines.Count > 0)
                    {
                        try { System.IO.File.AppendAllText("commerce_debug.txt", $"[{DateTime.Now}] RECOVERING LINES for {quote.Id}\n"); } catch {}
                        
                        foreach (var lineDto in dto.Lines)
                        {
                            var lineTotalHT = lineDto.Quantity * lineDto.UnitPrice;
                            var line = new QuoteLine
                            {
                                QuoteId = savedQuote.Id,
                                ProductId = lineDto.ProductId,
                                Description = lineDto.Description,
                                Date = lineDto.Date,
                                Quantity = lineDto.Quantity,
                                Unit = lineDto.Unit,
                                UnitPrice = lineDto.UnitPrice,
                                VATRate = lineDto.VATRate,
                                TotalHT = lineTotalHT
                            };
                            _context.QuoteLines.Add(line);
                        }
                    }
                    
                    if (needSave)
                    {
                        await _context.SaveChangesAsync();
                        _context.Entry(savedQuote).State = EntityState.Detached; // Detach again for clean reload
                    }
                }
            }
            
            // Reload to get Client info
            var reloaded = await GetQuoteByIdAsync(quote.Id);
            try { System.IO.File.AppendAllText("commerce_debug.txt", $"[{DateTime.Now}] RELOADED LineCount={reloaded?.Lines?.Count}\n"); } catch {}
            
            return reloaded ?? throw new Exception("Error retrieving saved quote (Reload failed)");
        }

        public async Task<bool> UpdateQuoteStatusAsync(Guid id, QuoteStatus status, string? rejectionReason = null)
        {
            var quote = await _context.Quotes.FindAsync(id);
            if (quote == null) return false;

            quote.Status = status;
            
            if (status == QuoteStatus.Sent) quote.DateSent = DateTime.UtcNow;
            if (status == QuoteStatus.Accepted) quote.DateAccepted = DateTime.UtcNow;
            if (status == QuoteStatus.Rejected) 
            {
                quote.DateRejected = DateTime.UtcNow;
                quote.RejectionReason = rejectionReason;
            }

            // History Log
            string action = status switch 
            {
                QuoteStatus.Sent => "Envoyé",
                QuoteStatus.Viewed => "Consulté",
                QuoteStatus.Accepted => "Accepté",
                QuoteStatus.Rejected => "Refusé",
                QuoteStatus.Draft => "Brouillon",
                _ => "Modifié"
            };

            var historyDetails = $"Statut passé à {action}";
            if (rejectionReason != null) historyDetails += $" ({rejectionReason})";
            if (status == QuoteStatus.Draft) historyDetails = "Devis remis en brouillon";

            _context.QuoteHistories.Add(new QuoteHistory 
            { 
                QuoteId = id, 
                Date = DateTime.UtcNow, 
                Action = "Changement de statut", 
                Details = historyDetails
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteQuoteAsync(Guid id)
        {
            var quote = await _context.Quotes.FindAsync(id);
            if (quote == null) return false;
            
            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]> GenerateQuotePdfAsync(QuoteDto quote)
        {
            return await _exportService.GenerateQuotePdfAsync(quote);
        }

        public async Task SendQuoteByEmailAsync(Guid id)
        {
            var quote = await _context.Quotes.Include(q => q.Client).FirstOrDefaultAsync(q => q.Id == id);
            if (quote == null) throw new Exception("Devis introuvable");
            if (quote.Client == null || string.IsNullOrEmpty(quote.Client.Email)) throw new Exception("Le client n'a pas d'adresse email valide");

            // FIXED: Use the specific domain requested
            var publicUrl = $"https://hippocampetransitmanager.com/portal/quote/{quote.PublicToken}";
            
            var subject = $"Devis {quote.Reference} - HIPPOCAMPE IMPORT EXPORT";
            
            // FIXED: New Company Info and Template
            var body = $@"
<div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto;'>
    <div style='text-align: center; margin-bottom: 20px;'>
       <!-- Logo if available online -->
       <h2 style='color: #2c3e50;'>HIPPOCAMPE IMPORT EXPORT</h2>
    </div>
    
    <p>Bonjour,</p>
    
    <p>Vous trouverez ci-joint votre devis <strong>{quote.Reference}</strong>.</p>
    
    <p>Nous vous remercions d'avoir fait appel à nos services et nous restons à votre entière disposition pour toute question.</p>
    
    <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0; border: 1px solid #e9ecef;'>
        <h3 style='margin-top: 0;'>Détails du devis</h3>
        <table style='width: 100%; border-collapse: collapse;'>
            <tr>
                <td style='padding: 5px 0; color: #6c757d;'>Référence :</td>
                <td style='padding: 5px 0; font-weight: bold; text-align: right;'>{quote.Reference}</td>
            </tr>
            <tr>
                <td style='padding: 5px 0; color: #6c757d;'>Date de validité :</td>
                <td style='padding: 5px 0; font-weight: bold; text-align: right;'>{quote.DateValidity:dd.MM.yyyy}</td>
            </tr>
             <tr>
                <td style='padding: 5px 0; color: #6c757d;'>Destinataire :</td>
                <td style='padding: 5px 0; font-weight: bold; text-align: right;'>{quote.Client.Nom} {quote.Client.Prenom}</td>
            </tr>
            <tr style='border-top: 1px solid #dee2e6;'>
                <td style='padding: 15px 0 5px 0; font-size: 1.1em;'>Total TTC :</td>
                <td style='padding: 15px 0 5px 0; font-weight: bold; font-size: 1.2em; color: #0d6efd; text-align: right;'>{quote.TotalTTC:N2} €</td>
            </tr>
        </table>
        
        <div style='text-align: center; margin-top: 25px;'>
            <a href='{publicUrl}' style='display: inline-block; background-color: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>Voir le devis</a>
        </div>
        <p style='text-align: center; margin-top: 10px; font-size: 12px; color: #6c757d;'>
            Cliquez sur le bouton ci-dessus pour visualiser, télécharger ou signer le devis en ligne.
        </p>
    </div>

    <p>Cordialement,</p>
    
    <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 14px; color: #555;'>
        <strong>HIPPOCAMPE IMPORT EXPORT - SAS</strong><br/>
        7 Rue Pascal 33370 Tresses<br/>
        Numéro de SIRET: 891909772 - Numéro de TVA: FR42891909772 - BORDEAUX<br/>
    </div>
</div>";

            try
            {
                await _emailService.SendEmailAsync(quote.Client.Email, subject, body);
                
                // Update status if Draft -> Sent
                if (quote.Status == QuoteStatus.Draft)
                {
                    quote.Status = QuoteStatus.Sent;
                    quote.DateSent = DateTime.UtcNow;

                    _context.QuoteHistories.Add(new QuoteHistory 
                    { 
                        QuoteId = id, 
                        Date = DateTime.UtcNow, 
                        Action = "Email envoyé", 
                        Details = $"Devis envoyé par email à {quote.Client.Email} avec succès" 
                    });

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _context.QuoteHistories.Add(new QuoteHistory 
                { 
                    QuoteId = id, 
                    Date = DateTime.UtcNow, 
                    Action = "Erreur Envoi Email", 
                    Details = $"L'envoi du devis par email a échoué: {ex.Message}" 
                });
                await _context.SaveChangesAsync();
                throw; // Re-throw to alert user
            }
        }

        // --- Helpers ---

        private static QuoteDto MapToDto(Quote q)
        {
            return new QuoteDto
            {
                Id = q.Id,
                Reference = q.Reference,
                ClientId = q.ClientId,
                ClientName = q.Client?.Nom ?? "Inconnu",
                ClientFirstname = q.Client?.Prenom ?? "",
                ClientPhone = q.Client?.TelephonePrincipal ?? "",
                ClientAddress = $"{q.Client?.AdressePrincipale} {q.Client?.CodePostal} {q.Client?.Ville}",
                ClientEmail = q.Client?.Email ?? "",
                DateCreated = q.DateCreated,
                DateValidity = q.DateValidity,
                Status = q.Status,
                Message = q.Message,
                PaymentTerms = q.PaymentTerms,
                FooterNote = q.FooterNote,
                DiscountValue = q.DiscountValue,
                DiscountType = q.DiscountType,
                DiscountBase = q.DiscountBase,
                DiscountScope = q.DiscountScope,
                
                // History
                DateSent = q.DateSent,
                DateAccepted = q.DateAccepted,
                DateRejected = q.DateRejected,
                DateViewed = q.DateViewed,
                RejectionReason = q.RejectionReason,
                
                PublicToken = q.PublicToken,
                // PublicUrl will be set by Controller/Service with Host info, or frontend
                TotalHT = q.TotalHT,
                TotalTVA = q.TotalTVA,
                TotalTTC = q.TotalTTC,
                Lines = q.Lines.Select(l => new QuoteLineDto
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    Description = l.Description,
                    Date = l.Date,
                    Quantity = l.Quantity,
                    Unit = l.Unit,
                    UnitPrice = l.UnitPrice,
                    VATRate = l.VATRate,
                    TotalHT = l.TotalHT
                }).ToList(),

                History = q.History.Select(h => new QuoteHistoryDto
                {
                    Date = h.Date,
                    Action = h.Action,
                    Details = h.Details,
                    User = h.UserId ?? "Système"
                }).OrderByDescending(h => h.Date).ToList()
            };
        }

        private async Task<string> GenerateQuoteReferenceAsync()
        {
            // Format: DEV-{Year}-{Seq} (e.g. DEV-2026-001)
            var year = DateTime.UtcNow.Year;
            var prefix = $"DEV-{year}-";
            
            Quote? lastQuote = await _context.Quotes
                .Where(q => q.Reference.StartsWith(prefix))
                .OrderByDescending(q => q.Reference)
                .FirstOrDefaultAsync();

            int nextSeq = 1;
            if (lastQuote != null)
            {
                string[] parts = lastQuote.Reference.Split('-');
                if (parts.Length == 3)
                {
                    if (int.TryParse(parts[2], out int lastSeq))
                    {
                        nextSeq = lastSeq + 1;
                    }
                }
            }

            return $"{prefix}{nextSeq.ToString("D3")}";
        }
    }
}
