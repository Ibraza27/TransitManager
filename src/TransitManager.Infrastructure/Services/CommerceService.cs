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
using TransitManager.Core.DTOs.Settings;
using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;

namespace TransitManager.Infrastructure.Services
{
    public class CommerceService : ICommerceService
    {
        private readonly TransitContext _context;
        private readonly IEmailService _emailService;
        private readonly IExportService _exportService;
        private readonly ISettingsService _settingsService;
        private readonly IDocumentService _documentService;
        private readonly IConfiguration _config;
        private readonly IServiceScopeFactory _scopeFactory;

        public CommerceService(
            TransitContext context,
            IEmailService emailService,
            IExportService exportService,
            ISettingsService settingsService,
            IDocumentService documentService,
            IConfiguration config,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _emailService = emailService;
            _exportService = exportService;
            _settingsService = settingsService;
            _documentService = documentService;
            _config = config;
            _scopeFactory = scopeFactory;
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
                var s = search.ToLower();
                query = query.Where(q => q.Reference.ToLower().Contains(s) 
                                      || (q.Client != null && (q.Client.Nom.ToLower().Contains(s) || q.Client.Prenom.ToLower().Contains(s)))
                                      || (q.GuestName != null && q.GuestName.ToLower().Contains(s))
                                      || (q.GuestEmail != null && q.GuestEmail.ToLower().Contains(s))
                                      || (q.GuestPhone != null && q.GuestPhone.Contains(s))
                                      );
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
            
            // Populate Linked Invoices
            var quoteIds = items.Select(q => q.Id).ToList();
            if (quoteIds.Any())
            {
                var linkedInvoices = await _context.Invoices
                    .Where(i => i.QuoteId.HasValue && quoteIds.Contains(i.QuoteId.Value))
                    .Select(i => new { i.QuoteId, i.Id, i.Reference })
                    .ToListAsync();
                    
                foreach(var item in items)
                {
                    var link = linkedInvoices.FirstOrDefault(l => l.QuoteId == item.Id);
                    if (link != null)
                    {
                        item.InvoiceId = link.Id;
                        item.InvoiceReference = link.Reference;
                    }
                }
            }

            return new PagedResult<QuoteDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<QuoteDto?> GetQuoteByIdAsync(Guid id)
        {
            var quote = await _context.Quotes
                .Include(q => q.Client)
                .Include(q => q.Lines)
                .Include(q => q.History)
                .Include(q => q.Client)
                .Include(q => q.Lines)
                .Include(q => q.History)
                .FirstOrDefaultAsync(q => q.Id == id);
            
            // Populate Linked Invoice
            QuoteDto? dto = quote == null ? null : MapToDto(quote);
            if (dto != null)
            {
                var linkedInvoice = await _context.Invoices
                    .Where(i => i.QuoteId == quote.Id)
                    .Select(i => new { i.Id, i.Reference })
                    .FirstOrDefaultAsync();
                    
                if (linkedInvoice != null)
                {
                    dto.InvoiceId = linkedInvoice.Id;
                    dto.InvoiceReference = linkedInvoice.Reference;
                }
            }
            
            // DEBUG LOG READ
            if (quote != null)
            {
                try {
                     var logMsg = $"[{DateTime.Now}] GetQuoteByIdAsync | ID={quote.Id} | ClientId={quote.ClientId} | GuestName='{quote.GuestName}' | GuestEmail='{quote.GuestEmail}'\n";
                     System.IO.File.AppendAllText("commerce_service_debug.txt", logMsg);
                } catch {}
            }

            return dto;
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
            // DEBUG: Log incoming payload
            try 
            {
                var debugLines = dto.Lines.Select((l, i) => new { Index=i, Pos=l.Position, Type=l.Type, Desc=l.Description, Qty=l.Quantity, Price=l.UnitPrice }).ToList();
                var logMsg = $"[{DateTime.Now}] CreateOrUpdateQuoteAsync | ID={dto.Id} | ClientId={dto.ClientId} | GuestName='{dto.GuestName}' | GuestEmail='{dto.GuestEmail}' | GuestPhone='{dto.GuestPhone}'\n";
                System.IO.File.AppendAllText("commerce_service_debug.txt", logMsg);
            } catch {}

            Quote quote;
            if (dto.Id.HasValue)
            {
                quote = await _context.Quotes.Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == dto.Id);
                if (quote == null) throw new Exception("Quote not found");
                
                // Update basic fields
                quote.ClientId = dto.ClientId;
                // Guest client fields
                quote.GuestName = dto.GuestName;
                quote.GuestEmail = dto.GuestEmail;
                quote.GuestPhone = dto.GuestPhone;
                
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
                    // Guest client fields
                    GuestName = dto.GuestName,
                    GuestEmail = dto.GuestEmail,
                    GuestPhone = dto.GuestPhone,
                    
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
            decimal runningSubtotal = 0;

            // TRUST LIST ORDER for calculation (Frontend sends list in visual order)
            int loopIndex = 0;
            foreach (var lineDto in dto.Lines) 
            {
                decimal lineTotalHT = 0;
                
                if (lineDto.Type == QuoteLineType.Subtotal)
                {
                    lineTotalHT = runningSubtotal;
                    runningSubtotal = 0; // Reset after usage
                }
                else if (lineDto.Type == QuoteLineType.Product)
                {
                    lineTotalHT = lineDto.Quantity * lineDto.UnitPrice;
                    var lineTVA = lineTotalHT * (lineDto.VATRate / 100m);
                    
                    grossHT += lineTotalHT;
                    grossTVA += lineTVA;
                    runningSubtotal += lineTotalHT;
                }

                // DEFENSIVE: Ensure UnitPrice/Quantity are consistent for Subtotals
                // This prevents issues if any system recalculates Total as Qty * Price
                decimal finalUnitPrice = lineDto.UnitPrice;
                decimal finalQuantity = lineDto.Quantity;

                if (lineDto.Type == QuoteLineType.Subtotal)
                {
                    finalUnitPrice = lineTotalHT;
                    finalQuantity = 1;
                }

                newLines.Add(new QuoteLine
                {
                    ProductId = lineDto.ProductId,
                    Description = lineDto.Description,
                    Date = lineDto.Date,
                    Quantity = finalQuantity,
                    Unit = lineDto.Unit,
                    UnitPrice = finalUnitPrice,
                    VATRate = lineDto.VATRate,
                    TotalHT = lineTotalHT,
                    Type = lineDto.Type,
                    Position = loopIndex++ // Force sequential position based on list order
                });

                 // DEBUG LOG
                try { System.IO.File.AppendAllText("commerce_debug_loop.txt", $"Line[{loopIndex-1}]: Type={lineDto.Type}, Amt={lineTotalHT}, RunSub={runningSubtotal}\n"); } catch {}
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
                    // FIX: Recover Guest Fields
                    savedQuote.GuestName = quote.GuestName;
                    savedQuote.GuestEmail = quote.GuestEmail;
                    savedQuote.GuestPhone = quote.GuestPhone;

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
                        
                        // IMPORTANT: Must re-calculate runningSubtotal here too!
                        decimal recoverRunningSubtotal = 0;
                        int recoverIndex = 0;

                        foreach (var lineDto in dto.Lines)
                        {
                            decimal lineTotalHT = 0;

                            if (lineDto.Type == QuoteLineType.Subtotal)
                            {
                                lineTotalHT = recoverRunningSubtotal;
                                recoverRunningSubtotal = 0;
                            }
                            else if (lineDto.Type == QuoteLineType.Product)
                            {
                                lineTotalHT = lineDto.Quantity * lineDto.UnitPrice;
                                recoverRunningSubtotal += lineTotalHT;
                            }

                            // DEFENSIVE: consistency for Subtotals
                            decimal finalUnitPrice = lineDto.UnitPrice;
                            decimal finalQuantity = lineDto.Quantity;

                            if (lineDto.Type == QuoteLineType.Subtotal)
                            {
                                finalUnitPrice = lineTotalHT;
                                finalQuantity = 1;
                            }

                            var line = new QuoteLine
                            {
                                QuoteId = savedQuote.Id,
                                ProductId = lineDto.ProductId,
                                Description = lineDto.Description,
                                Date = lineDto.Date,
                                Quantity = finalQuantity,
                                Unit = lineDto.Unit,
                                UnitPrice = finalUnitPrice,
                                VATRate = lineDto.VATRate,
                                TotalHT = lineTotalHT,
                                Type = lineDto.Type,
                                Position = recoverIndex++
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
            
            // Synchronize with linked Invoice
            // FIX: Reload with AsNoTracking to ensure Sync uses fresh DB data (avoids "Save Twice" bug)
            var finalQuote = await _context.Quotes.AsNoTracking().Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == quote.Id);
            if (finalQuote != null)
            {
                await SyncQuoteToInvoiceAsync(finalQuote);
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
                QuoteStatus.ChangeRequested => "Modification demandée",
                _ => "Modifié"
            };

            var historyDetails = status == QuoteStatus.ChangeRequested ? $"Demande de modification" : $"Statut passé à {action}";
            if (rejectionReason != null && status == QuoteStatus.ChangeRequested) historyDetails += $": {rejectionReason}";
            else if (rejectionReason != null) historyDetails += $" ({rejectionReason})";
            
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

        public async Task SendQuoteByEmailAsync(Guid id, string? subject = null, string? body = null, List<Guid>? attachmentIds = null, List<string>? ccEmails = null, List<string>? recipients = null)
        {
            var quote = await _context.Quotes.Include(q => q.Client).Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == id);
            if (quote == null) throw new Exception("Devis introuvable");
            
            string recipientEmail = quote.Client?.Email ?? quote.GuestEmail;
            // Display: Use GuestName if present, else GuestEmail. Remove generic "Client" fallback if empty.
            string recipientName = quote.Client != null 
                ? $"{quote.Client.Nom} {quote.Client.Prenom}" 
                : (!string.IsNullOrWhiteSpace(quote.GuestName) ? quote.GuestName : quote.GuestEmail);
            
            if (string.IsNullOrEmpty(recipientEmail)) throw new Exception("Aucune adresse email valide (ni client, ni invité)");

            // Load Settings
            var company = await _settingsService.GetSettingAsync<TransitManager.Core.DTOs.Settings.CompanyProfileDto>("CompanyProfile", new());
            
            // Generate PDF
            var pdfBytes = await _exportService.GenerateQuotePdfAsync(MapToDto(quote));
            var pdfName = $"Devis_{quote.Reference}.pdf";

            // Prepare Attachments
            var attachments = new List<(string Name, byte[] Content)>();
            attachments.Add((pdfName, pdfBytes));

            // Load Additional Attachments (Temp Files)
            if (attachmentIds != null && attachmentIds.Any())
            {
                foreach (var attId in attachmentIds)
                {
                    try 
                    {
                        var tempFile = await _documentService.GetTempDocumentAsync(attId);
                        if (tempFile != null)
                        {
                            using var ms = new MemoryStream();
                            await tempFile.Value.FileStream.CopyToAsync(ms);
                            attachments.Add((tempFile.Value.FileName, ms.ToArray()));
                        }
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Error loading temp attachment {attId}: {ex.Message}");
                    }
                }
            }

            // Public Link
            var publicUrl = $"https://hippocampetransitmanager.com/portal/quote/{quote.PublicToken}";
            
            // Build Final HTML Content
            // 1. Determine User Message (Custom or Default)
            string userMessagePart;
            if (!string.IsNullOrWhiteSpace(body))
            {
                // Preserve newlines if plain text
                userMessagePart = body.Contains("<") && body.Contains(">") ? body : body.Replace("\n", "<br/>");
            }
            else
            {
                userMessagePart = $@"
                    <p>Bonjour,</p>
                    <p>Vous trouverez ci-joint votre devis <strong>{quote.Reference}</strong>.</p>
                    <p>Nous vous remercions d'avoir fait appel à nos services et nous restons à votre entière disposition pour toute question.</p>";
            }

            // 2. Wrap in Standard Template
            var finalHtml = $@"
<div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto;'>
    <div style='text-align: center; margin-bottom: 20px;'>
       <h2 style='color: #2c3e50;'>{company.CompanyName}</h2>
    </div>
    
    <div style='margin-bottom: 30px;'>
        {userMessagePart}
    </div>
    
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
                 <td style='padding: 5px 0; font-weight: bold; text-align: right;'>{recipientName}</td>
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
        <strong>{company.CompanyName} - {company.LegalStatus}</strong><br/>
        {company.Address}<br/>
        {company.ZipCode} {company.City}<br/>
        Tél: {company.Phone}<br/>
        {(!string.IsNullOrEmpty(company.Website) ? $"Site Web: <a href='{company.Website}'>{company.Website}</a><br/>" : "")}
    </div>
</div>";

            // Default Subject
            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = $"Devis {quote.Reference} - {company.CompanyName}";
            }

            try
            {
                // Determine To addresses: use recipients if provided, otherwise default to client/guest email
                string toEmails = recipientEmail;
                if (recipients != null && recipients.Any())
                {
                    toEmails = string.Join(",", recipients);
                }
                
                await _emailService.SendEmailAsync(toEmails, subject, finalHtml, attachments, ccEmails, "contact@hippocampeimportexport.com");
                
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
                        Details = $"Devis envoyé par email à {toEmails} avec succès" 
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
                throw; 
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
                ClientName = q.Client?.Nom,
                ClientFirstname = q.Client?.Prenom ?? "",
                ClientPhone = q.Client?.TelephonePrincipal ?? "",
                ClientAddress = q.Client != null ? $"{q.Client.AdressePrincipale} {q.Client.CodePostal} {q.Client.Ville}" : "",
                ClientEmail = q.Client?.Email ?? "",
                // Guest client fields
                GuestName = q.GuestName,
                GuestEmail = q.GuestEmail,
                GuestPhone = q.GuestPhone,
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
                Lines = q.Lines.OrderBy(l => l.Position).Select(l => new QuoteLineDto
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    Description = l.Description,
                    Date = l.Date,
                    Quantity = l.Quantity,
                    Unit = l.Unit,
                    UnitPrice = l.UnitPrice,
                    VATRate = l.VATRate,
                    TotalHT = l.TotalHT,
                    Type = l.Type,
                    Position = l.Position
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
        // --- Invoices ---

        public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(string? search, Guid? clientId, string? status, int page = 1, int pageSize = 20)
        {
            var query = _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Client)
                .Include(i => i.Lines)
                .Include(i => i.History)
                .Include(i => i.Quote) // Include Quote for Reference
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(q => q.Reference.ToLower().Contains(s) 
                                      || (q.Client != null && (q.Client.Nom.ToLower().Contains(s) || q.Client.Prenom.ToLower().Contains(s)))
                                      || (q.GuestName != null && q.GuestName.ToLower().Contains(s))
                                      || (q.GuestEmail != null && q.GuestEmail.ToLower().Contains(s))
                                      || (q.GuestPhone != null && q.GuestPhone.Contains(s))
                                      );
            }
            if (clientId.HasValue)
            {
                query = query.Where(q => q.ClientId == clientId.Value);
            }
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<InvoiceStatus>(status, true, out var statusEnum))
            {
                query = query.Where(q => q.Status == statusEnum);
            }

            var total = await query.CountAsync();
            var entities = await query.OrderByDescending(q => q.Reference)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var items = entities.Select(i => MapInvoiceToDto(i)).ToList();

            return new PagedResult<InvoiceDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Client)
                .Include(i => i.Lines)
                .Include(i => i.History)
                .Include(i => i.Quote)
                .FirstOrDefaultAsync(i => i.Id == id);
            return invoice == null ? null : MapInvoiceToDto(invoice);
        }

        public async Task<InvoiceDto?> GetInvoiceByTokenAsync(Guid token)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Lines)
                .Include(i => i.History)
                .FirstOrDefaultAsync(i => i.PublicToken == token);
            
            if (invoice != null && invoice.Status == InvoiceStatus.Sent)
            {
                invoice.Status = InvoiceStatus.Viewed;
                invoice.DateViewed = DateTime.UtcNow;
                
                _context.InvoiceHistories.Add(new InvoiceHistory 
                { 
                    InvoiceId = invoice.Id, 
                    Date = DateTime.UtcNow, 
                    Action = "Consulté en ligne", 
                    Details = "Facture visionnée via le lien public" 
                });
                
                await _context.SaveChangesAsync();
            }

            return invoice == null ? null : MapInvoiceToDto(invoice);
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto dto)
        {
            var settings = await _settingsService.GetSettingAsync<BillingSettingsDto>("BillingSettings", new());

            // Apply defaults if not provided in DTO
            if (string.IsNullOrWhiteSpace(dto.PaymentTerms)) dto.PaymentTerms = settings.DefaultPaymentTerms;
            if (string.IsNullOrWhiteSpace(dto.FooterNote)) dto.FooterNote = settings.DefaultFooterNote;

            var invoice = new Invoice
            {
                Reference = await GenerateInvoiceReferenceAsync(),
                ClientId = dto.ClientId,
                GuestName = dto.GuestName,
                GuestEmail = dto.GuestEmail,
                GuestPhone = dto.GuestPhone,
                DateCreated = dto.DateCreated,
                DueDate = dto.DueDate,
                Message = dto.Message,
                PaymentTerms = dto.PaymentTerms,
                FooterNote = dto.FooterNote,
                DiscountValue = dto.DiscountValue,
                DiscountType = dto.DiscountType,
                DiscountBase = dto.DiscountBase,
                DiscountScope = dto.DiscountScope,
                Status = InvoiceStatus.Draft,
                PublicToken = Guid.NewGuid()
            };

            // Add Lines
            decimal grossHT = 0;
            decimal grossTVA = 0;
            if (dto.Lines != null)
            {
                int loopPos = 0;
                foreach (var lineDto in dto.Lines)
                {
                    decimal lineHT = lineDto.Quantity * lineDto.UnitPrice;
                    
                    if (lineDto.Type == QuoteLineType.Product)
                    {
                        grossHT += lineHT;
                        grossTVA += lineHT * (lineDto.VATRate / 100m);
                    }

                    invoice.Lines.Add(new InvoiceLine
                    {
                        ProductId = lineDto.ProductId,
                        Description = lineDto.Description,
                        Date = lineDto.Date,
                        Quantity = lineDto.Quantity,
                        Unit = lineDto.Unit,
                        UnitPrice = lineDto.UnitPrice,
                        VATRate = lineDto.VATRate,
                        TotalHT = lineHT,
                        Type = lineDto.Type,
                        Position = lineDto.Position != 0 ? lineDto.Position : loopPos++
                    });
                }
            }

            // Calculations
            decimal discountAmount = 0;
            if (invoice.DiscountType == DiscountType.Percent)
                discountAmount = grossHT * (invoice.DiscountValue / 100m);
            else
                discountAmount = invoice.DiscountValue;

            if (discountAmount > grossHT) discountAmount = grossHT;
            
            invoice.TotalHT = grossHT - discountAmount;
            decimal discountRatio = grossHT == 0 ? 0 : (discountAmount / grossHT);
            invoice.TotalTVA = grossTVA * (1 - discountRatio);
            invoice.TotalTTC = invoice.TotalHT + invoice.TotalTVA;

            _context.Invoices.Add(invoice);
            
            _context.InvoiceHistories.Add(new InvoiceHistory 
            { 
                InvoiceId = invoice.Id, 
                Action = "Création", 
                Details = "Facture créée manuellement" 
            });

            try 
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // DETECT PHANTOM INSERT (SQLite/EF Core issue)
                var exists = await _context.Invoices.AnyAsync(i => i.Id == invoice.Id);
                if (!exists) throw; 

                // If it exists, it was likely inserted successfully despite the exception
                _context.Entry(invoice).State = EntityState.Detached;
                var saved = await _context.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == invoice.Id);
                if (saved != null) return MapInvoiceToDto(saved);
                throw;
            }

            return MapInvoiceToDto(invoice);
        }

        // UpdateInvoiceAsync method moved below DeleteInvoiceAsync with complete try-catch

        public async Task<bool> UpdateInvoiceStatusAsync(Guid id, InvoiceStatus status)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return false;

            var oldStatus = invoice.Status;
            invoice.Status = status;

            if (status == InvoiceStatus.Sent && oldStatus != InvoiceStatus.Sent) invoice.DateSent = DateTime.UtcNow;
            if (status == InvoiceStatus.Paid && oldStatus != InvoiceStatus.Paid) invoice.DatePaid = DateTime.UtcNow;

            _context.InvoiceHistories.Add(new InvoiceHistory 
            { 
               InvoiceId = id, 
               Action = "Changement statut", 
               Details = $"Statut passé de {oldStatus} à {status}" 
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteInvoiceAsync(Guid id)
        {
             var invoice = await _context.Invoices.FindAsync(id);
             if (invoice != null)
             {
                 _context.Invoices.Remove(invoice);
                 await _context.SaveChangesAsync();
             }
        }

        public async Task<InvoiceDto> UpdateInvoiceAsync(Guid id, UpdateInvoiceDto dto)
        {
            try
            {
                // DEBUG LOG
                try {
                     var lineCount = dto.Lines?.Count ?? 0;
                     System.IO.File.AppendAllText("commerce_debug_update.txt", $"[{DateTime.Now}] UpdateInvoiceAsync ID={id}: Received {lineCount} lines.\n");
                } catch {}

                // STEP 1: Load invoice WITHOUT tracking to avoid ChangeTracker pollution
                var existingInvoice = await _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == id);
                    
                if (existingInvoice == null) throw new Exception("Facture introuvable");

                // STEP 2: Delete existing lines DIRECTLY in DB (bypass tracking)
                await _context.InvoiceLines.Where(l => l.InvoiceId == id).ExecuteDeleteAsync();
                
                try { System.IO.File.AppendAllText("commerce_debug_update.txt", $"[{DateTime.Now}] Lines deleted via ExecuteDeleteAsync.\n"); } catch {}

                // STEP 3: Calculate new totals
                decimal grossHT = 0;
                decimal grossTVA = 0;
                var newLines = new List<InvoiceLine>();
                
                if(dto.Lines != null)
                {
                    foreach(var lineDto in dto.Lines)
                    {
                        decimal lineHT = lineDto.Quantity * lineDto.UnitPrice;
                        
                        if (lineDto.Type == QuoteLineType.Product)
                        {
                            grossHT += lineHT;
                            grossTVA += lineHT * (lineDto.VATRate / 100m);
                        }

                        newLines.Add(new InvoiceLine
                        {
                            Id = Guid.NewGuid(), // Force new ID
                            InvoiceId = id,
                            Description = lineDto.Description,
                            Date = lineDto.Date, // FIX: Include Date field
                            Quantity = lineDto.Quantity,
                            UnitPrice = lineDto.UnitPrice,
                            VATRate = lineDto.VATRate,
                            Unit = lineDto.Unit,
                            TotalHT = lineHT,
                            Type = lineDto.Type,
                            Position = lineDto.Position,
                            ProductId = lineDto.ProductId
                        });
                    }
                }

                // Calculate discount
                decimal discountAmount = 0;
                if (dto.DiscountType == DiscountType.Percent)
                    discountAmount = grossHT * (dto.DiscountValue / 100m);
                else
                    discountAmount = dto.DiscountValue;

                if (discountAmount > grossHT) discountAmount = grossHT;

                decimal finalTotalHT = grossHT - discountAmount;
                decimal discountRatio = grossHT == 0 ? 0 : (discountAmount / grossHT);
                decimal finalTotalTVA = grossTVA * (1 - discountRatio);
                decimal finalTotalTTC = finalTotalHT + finalTotalTVA;

                // FIX: Convert dates to UTC (ExecuteUpdateAsync bypasses TransitContext.SaveChangesAsync conversion)
                var dateCreatedUtc = dto.DateCreated.Kind == DateTimeKind.Utc 
                    ? dto.DateCreated 
                    : DateTime.SpecifyKind(dto.DateCreated, DateTimeKind.Local).ToUniversalTime();
                var dueDateUtc = dto.DueDate.Kind == DateTimeKind.Utc 
                    ? dto.DueDate 
                    : DateTime.SpecifyKind(dto.DueDate, DateTimeKind.Local).ToUniversalTime();

                // STEP 4: Use ExecuteUpdateAsync for the Invoice header DIRECTLY in DB (bypass tracking completely)
                await _context.Invoices
                    .Where(i => i.Id == id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(i => i.Message, dto.Message)
                        .SetProperty(i => i.PaymentTerms, dto.PaymentTerms)
                        .SetProperty(i => i.FooterNote, dto.FooterNote)
                        .SetProperty(i => i.DateCreated, dateCreatedUtc)
                        .SetProperty(i => i.DueDate, dueDateUtc)
                        .SetProperty(i => i.DiscountValue, dto.DiscountValue)
                        .SetProperty(i => i.DiscountType, dto.DiscountType)
                        .SetProperty(i => i.DiscountBase, dto.DiscountBase)
                        .SetProperty(i => i.DiscountScope, dto.DiscountScope)
                        .SetProperty(i => i.ClientId, dto.ClientId)
                        .SetProperty(i => i.GuestName, dto.GuestName)
                        .SetProperty(i => i.GuestEmail, dto.GuestEmail)
                        .SetProperty(i => i.GuestPhone, dto.GuestPhone)
                        .SetProperty(i => i.TotalHT, finalTotalHT)
                        .SetProperty(i => i.TotalTVA, finalTotalTVA)
                        .SetProperty(i => i.TotalTTC, finalTotalTTC)
                    );

                try { System.IO.File.AppendAllText("commerce_debug_update.txt", $"[{DateTime.Now}] Invoice header updated via ExecuteUpdateAsync.\n"); } catch {}

                // STEP 5: Insert new lines directly
                if (newLines.Count > 0)
                {
                    _context.InvoiceLines.AddRange(newLines);
                    await _context.SaveChangesAsync();
                }
                
                try { System.IO.File.AppendAllText("commerce_debug_update.txt", $"[{DateTime.Now}] {newLines.Count} lines inserted. SUCCESS!\n"); } catch {}

                // STEP 6: Synchronize with linked Quote if applicable
                if (existingInvoice.QuoteId.HasValue)
                {
                    // Reload fresh data for sync
                    var freshInvoice = await _context.Invoices
                        .AsNoTracking()
                        .Include(i => i.Lines)
                        .FirstOrDefaultAsync(i => i.Id == id);
                    if (freshInvoice != null)
                    {
                        await SyncInvoiceToQuoteAsync(freshInvoice);
                    }
                }

                // STEP 7: Reload and return the final state
                var finalInvoice = await _context.Invoices
                    .AsNoTracking()
                    .Include(i => i.Lines)
                    .Include(i => i.Client)
                    .Include(i => i.History)
                    .FirstOrDefaultAsync(i => i.Id == id);
                    
                if (finalInvoice == null) throw new Exception("Facture introuvable après mise à jour");
                
                return MapInvoiceToDto(finalInvoice);
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText("commerce_invoice_error.txt", $"[{DateTime.Now}] UpdateInvoiceAsync ERROR: {ex}\n"); } catch {}
                throw;
            }
        }



        public async Task<InvoiceDto> ConvertQuoteToInvoiceAsync(Guid quoteId)
        {
            var quote = await _context.Quotes.Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == quoteId);
            if (quote == null) throw new Exception("Devis introuvable");

            var settings = await _settingsService.GetSettingAsync<InvoiceSettingsDto>("InvoiceSettings", new());

            var invoice = new Invoice
            {
                Reference = await GenerateInvoiceReferenceAsync(),
                ClientId = quote.ClientId,
                QuoteId = quote.Id,
                DateCreated = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30), // Default 30 days
                Status = InvoiceStatus.Draft,
                Message = quote.Message,
                PaymentTerms = settings.DefaultPaymentTerms, // ALWAYS apply default settings for fresh Invoice as requested
                FooterNote = settings.DefaultFooterNote, // ALWAYS apply default settings for fresh Invoice
                DiscountValue = quote.DiscountValue,
                DiscountType = quote.DiscountType,
                DiscountBase = quote.DiscountBase,
                DiscountScope = quote.DiscountScope,
                TotalHT = quote.TotalHT,
                TotalTVA = quote.TotalTVA,
                TotalTTC = quote.TotalTTC,
                PublicToken = Guid.NewGuid(),
                // FIX: Copy Guest Data
                GuestName = quote.GuestName,
                GuestEmail = quote.GuestEmail,
                GuestPhone = quote.GuestPhone
            };

            // Copy Lines
            foreach(var qLine in quote.Lines.OrderBy(l => l.Position))
            {
                invoice.Lines.Add(new InvoiceLine
                {
                    ProductId = qLine.ProductId,
                    Description = qLine.Description,
                    Date = qLine.Date,
                    Quantity = qLine.Quantity,
                    Unit = qLine.Unit,
                    UnitPrice = qLine.UnitPrice,
                    VATRate = qLine.VATRate,
                    TotalHT = qLine.TotalHT,
                    Type = qLine.Type,
                    Position = qLine.Position
                });
            }

            _context.Invoices.Add(invoice);
            
            // Update Quote Status
            quote.Status = QuoteStatus.Converted;
            _context.QuoteHistories.Add(new QuoteHistory 
            { 
                QuoteId = quote.Id, 
                Action = "Converti", 
                Details = $"Devis converti en facture {invoice.Reference}" 
            });

            _context.InvoiceHistories.Add(new InvoiceHistory 
            { 
                InvoiceId = invoice.Id, 
                Action = "Création", 
                Details = $"Facture créée depuis le devis {quote.Reference}" 
            });

            await _context.SaveChangesAsync();
            return MapInvoiceToDto(invoice);
        }


        public async Task SendInvoiceByEmailAsync(Guid id, string? subject = null, string? body = null, List<Guid>? attachmentIds = null, List<string>? ccEmails = null, List<string>? recipients = null)
        {
             var invoice = await _context.Invoices
                 .Include(i => i.Client)
                 .Include(i => i.Lines)
                 .FirstOrDefaultAsync(i => i.Id == id);
             if (invoice == null) throw new Exception("Facture introuvable");

             string recipientEmail = invoice.Client?.Email ?? invoice.GuestEmail;
             string recipientName = invoice.Client != null 
                ? $"{invoice.Client.Nom} {invoice.Client.Prenom}" 
                : (!string.IsNullOrWhiteSpace(invoice.GuestName) ? invoice.GuestName : invoice.GuestEmail);

             if (string.IsNullOrEmpty(recipientEmail)) throw new Exception("Aucune adresse email valide (ni client, ni invité)");

             // Determine Recipients
             var toEmails = new List<string>();
             if (recipients != null && recipients.Any())
             {
                 toEmails.AddRange(recipients);
             }
             else
             {
                 toEmails.Add(recipientEmail);
             }
             if (!toEmails.Any()) throw new Exception("Aucun destinataire défini.");

             // Generate PDF
             var pdfBytes = await _exportService.GenerateInvoicePdfAsync(MapInvoiceToDto(invoice));
             
             // Create Attachment list with PDF
             var attachments = new List<(string Name, byte[] Content)>
             {
                 ($"Facture_{invoice.Reference}.pdf", pdfBytes)
             };
             
             // Load additional attachments from temp uploads
             if (attachmentIds != null && attachmentIds.Any())
             {
                 foreach (var tempId in attachmentIds)
                 {
                     var tempFile = await _documentService.GetTempDocumentAsync(tempId);
                     if (tempFile.HasValue)
                     {
                         using var ms = new MemoryStream();
                         await tempFile.Value.FileStream.CopyToAsync(ms);
                         attachments.Add((tempFile.Value.FileName, ms.ToArray()));
                         await tempFile.Value.FileStream.DisposeAsync();
                     }
                 }
             }

             // Build Rich HTML Body (Match Quote Email Style)
             var publicLink = $"https://hippocampetransitmanager.com/portal/invoice/{invoice.PublicToken}";
             var company = await _settingsService.GetSettingAsync<Core.DTOs.Settings.CompanyProfileDto>("CompanyProfile", new());

             // Determine User Message (Custom or Default)
             string userMessagePart;
             if (!string.IsNullOrWhiteSpace(body))
             {
                 // Preserve newlines if plain text
                 userMessagePart = body.Contains("<") && body.Contains(">") ? body : body.Replace("\n", "<br/>");
             }
             else
             {
                 userMessagePart = $@"
                     <p>Bonjour,</p>
                     <p>Vous trouverez ci-joint votre facture <strong>{invoice.Reference}</strong>.</p>
                     <p>Nous vous remercions d'avoir choisi notre solution !</p>";
             }

             // Wrap in Standard Template (Same as Quote Email)
             var htmlBody = $@"
<div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto;'>
    <div style='text-align: center; margin-bottom: 20px;'>
       <h2 style='color: #2c3e50;'>{company.CompanyName}</h2>
    </div>
    
    <div style='margin-bottom: 30px;'>
        {userMessagePart}
    </div>
    
    <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 25px 0; border: 1px solid #e9ecef;'>
        <h3 style='margin-top: 0;'>Détails de la facture</h3>
        <table style='width: 100%; border-collapse: collapse;'>
            <tr>
                <td style='padding: 5px 0; color: #6c757d;'>Référence :</td>
                <td style='padding: 5px 0; font-weight: bold; text-align: right;'>{invoice.Reference}</td>
            </tr>
            <tr>
                <td style='padding: 5px 0; color: #6c757d;'>Date d'émission :</td>
                <td style='padding: 5px 0; font-weight: bold; text-align: right;'>{invoice.DateCreated:dd.MM.yyyy}</td>
            </tr>
            <tr>
                <td style='padding: 5px 0; color: #6c757d;'>Date d'échéance :</td>
                <td style='padding: 5px 0; font-weight: bold; text-align: right; color: #dc3545;'>{invoice.DueDate:dd.MM.yyyy}</td>
            </tr>
             <tr>
                 <td style='padding: 5px 0; color: #6c757d;'>Destinataire :</td>
                 <td style='padding: 5px 0; font-weight: bold; text-align: right;'>{recipientName}</td>
             </tr>
            <tr style='border-top: 1px solid #dee2e6;'>
                <td style='padding: 15px 0 5px 0; font-size: 1.1em;'>Total TTC :</td>
                <td style='padding: 15px 0 5px 0; font-weight: bold; font-size: 1.2em; color: #0d6efd; text-align: right;'>{invoice.TotalTTC:N2} €</td>
            </tr>
        </table>
        
        <div style='text-align: center; margin-top: 25px;'>
            <a href='{publicLink}' style='display: inline-block; background-color: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>Voir la facture</a>
        </div>
        <p style='text-align: center; margin-top: 10px; font-size: 12px; color: #6c757d;'>
            Cliquez sur le bouton ci-dessus pour visualiser ou télécharger la facture.
        </p>
    </div>

    <p>Cordialement,</p>
    
        <strong>{company.CompanyName} - {company.LegalStatus}</strong><br/>
        {company.Address}<br/>
        {company.ZipCode} {company.City}<br/>
        Tél: {company.Phone}<br/>
        {(!string.IsNullOrEmpty(company.Website) ? $"Site Web: <a href='{company.Website}'>{company.Website}</a><br/>" : "")}
    </div>
</div>";

             // Send to joined recipients (comma sep if SendEmailAsync supports it? No, usually it takes one or we loop. 
             // Core IEmailService usually takes string `to`. If it supports comma, good.
             // If not, we might need a loop or change IEmailService. 
             // Assuming IEmailService.SendEmailAsync takes comma separated string or list?
             // Checking line 1105 usage: `invoice.Client.Email`.
             // I'll join them by comma, assuming standard SMTP/Service handling.
             var toAddress = string.Join(",", toEmails);

             await _emailService.SendEmailAsync(toAddress, subject, htmlBody, attachments, ccEmails, "contact@hippocampeimportexport.com");

             invoice.DateSent = DateTime.UtcNow;
             if(invoice.Status == InvoiceStatus.Draft) invoice.Status = InvoiceStatus.Sent;
             
             _context.InvoiceHistories.Add(new InvoiceHistory { InvoiceId = id, Action = "Envoyée", Details = $"Envoyée par email à {toAddress}" });
             await _context.SaveChangesAsync();
        }

        public async Task SendPaymentReminderAsync(Guid id, string? subject = null, string? body = null, List<Guid>? attachmentIds = null, List<string>? ccEmails = null, List<string>? recipients = null)
        {
             var invoice = await _context.Invoices
                 .Include(i => i.Client)
                 .Include(i => i.Lines)
                 .FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) throw new Exception("Facture introuvable");

             // Determine Recipients
             var toEmails = new List<string>();
             if (recipients != null && recipients.Any())
             {
                 toEmails.AddRange(recipients);
             }
             else
             {
                 toEmails.Add(invoice.Client.Email);
             }
             if (!toEmails.Any()) throw new Exception("Aucun destinataire défini.");

            // Similar sending logic
             var company = await _settingsService.GetSettingAsync<TransitManager.Core.DTOs.Settings.CompanyProfileDto>("CompanyProfile", new());
             
             // Maybe attach invoice again? Yes usually.
             var pdfBytes = await GenerateInvoicePdfAsync(MapInvoiceToDto(invoice));
             var attachments = new List<(string Name, byte[] Content)> 
             {
                 ($"Facture_{invoice.Reference}.pdf", pdfBytes)
             };

             if (string.IsNullOrWhiteSpace(subject)) subject = $"Rappel de paiement - Facture {invoice.Reference} - {invoice.TotalTTC:N2} €";

              var publicLink = $"https://hippocampetransitmanager.com/portal/invoice/{invoice.PublicToken}";
              
             // Determine User Message (Custom or Default)
             string userMessagePart;
             if (!string.IsNullOrWhiteSpace(body))
             {
                 userMessagePart = body.Contains("<") && body.Contains(">") ? body : body.Replace("\n", "<br/>");
             }
             else
             {
                 userMessagePart = @"
                     <p>Nous nous permettons de vous rappeler que votre facture reste impayée à ce jour.</p>
                     <p>Merci de bien vouloir procéder au règlement dans les meilleurs délais.</p>";
             }

             // Rich Reminder Body (Improved Layout)
             // Determine if we need a greeting (avoid duplicate Bonjour)
             var hasGreeting = !string.IsNullOrWhiteSpace(body) && 
                 (body.ToLower().Contains("bonjour") || body.ToLower().Contains("madame") || body.ToLower().Contains("monsieur"));
             var greetingHtml = hasGreeting ? "" : "<p>Bonjour,</p>";
             
             var htmlBody = $@"
<div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto;'>
    <div style='text-align: center; margin-bottom: 20px;'>
       <h2 style='color: #dc3545;'>Rappel de Paiement</h2>
    </div>
    
    {greetingHtml}
    
    <div style='margin-bottom: 30px;'>
        {userMessagePart}
    </div>
    
    <div style='background: #fff3cd; padding: 20px; border-radius: 8px; margin: 25px 0; border: 1px solid #ffc107;'>
        <h3 style='margin-top: 0; color: #856404;'>Détails de la facture</h3>
        <table style='width: 100%; border-collapse: collapse;'>
            <tr>
                <td style='padding: 5px 0; color: #856404;'>Référence :</td>
                <td style='padding: 5px 0; font-weight: bold; text-align: right;'>{invoice.Reference}</td>
            </tr>
            <tr>
                <td style='padding: 5px 0; color: #856404;'>Date d'échéance :</td>
                <td style='padding: 5px 0; font-weight: bold; text-align: right; color: #dc3545;'>{invoice.DueDate:dd.MM.yyyy}</td>
            </tr>
            <tr style='border-top: 1px solid #c9a927;'>
                <td style='padding: 15px 0 5px 0; font-size: 1.1em;'>Montant dû :</td>
                <td style='padding: 15px 0 5px 0; font-weight: bold; font-size: 1.2em; color: #dc3545; text-align: right;'>{invoice.TotalTTC:N2} €</td>
            </tr>
        </table>
        
        <div style='text-align: center; margin-top: 25px;'>
            <a href='{publicLink}' style='display: inline-block; background-color: #dc3545; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; font-size: 16px;'>Régler la facture</a>
        </div>
        <p style='text-align: center; margin-top: 10px; font-size: 12px; color: #856404;'>
            Cliquez sur le bouton ci-dessus pour effectuer le règlement en ligne.
        </p>
    </div>

    <p>Nous vous remercions de votre compréhension.</p>
    <p>Cordialement,</p>
    
        <strong>{company.CompanyName} - {company.LegalStatus}</strong><br/>
        {company.Address}<br/>
        {company.ZipCode} {company.City}<br/>
        Tél: {company.Phone}<br/>
        {(!string.IsNullOrEmpty(company.Website) ? $"Site Web: <a href='{company.Website}'>{company.Website}</a><br/>" : "")}
    </div>
</div>";

             var toAddress = string.Join(",", toEmails);

             await _emailService.SendEmailAsync(toAddress, subject, htmlBody, attachments, ccEmails, "contact@hippocampeimportexport.com");

             invoice.ReminderCount++;
             invoice.LastReminderSent = DateTime.UtcNow;
             
             _context.InvoiceHistories.Add(new InvoiceHistory { InvoiceId = id, Action = "Rappel envoyé", Details = $"Rappel #{invoice.ReminderCount} envoyé à {toAddress}" });
             await _context.SaveChangesAsync();
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(InvoiceDto invoice)
        {
            // We need a GenerateInvoicePdf in ExportService too, but for now we can maybe reuse/mock or add it.
            // Assumption: ExportService needs update too or we use generic PDF gen?
            // User didn't explicitly ask for PDF layout changes, but Invoice needs to say "Facture".
            // We'll likely need to update ExportService later. For now, let's pretend ExportService has it or use a placeholder logic.
            // Actually, I should check ExportService. Creating a placeholder call for now.
             return await _exportService.GenerateInvoicePdfAsync(invoice);
        }
        
        // Helpers
        private InvoiceDto MapInvoiceToDto(Invoice i)
        {
            return new InvoiceDto
            {
                Id = i.Id,
                Reference = i.Reference,
                ClientId = i.ClientId,
                ClientName = i.Client?.Nom,
                ClientFirstname = i.Client?.Prenom ?? "",
                ClientEmail = i.Client?.Email ?? "",
                ClientAddress = i.Client?.AdressePrincipale ?? "",
                ClientPhone = i.Client?.TelephonePrincipal ?? "",
                // Guest client fields
                GuestName = i.GuestName,
                GuestEmail = i.GuestEmail,
                GuestPhone = i.GuestPhone,
                DateCreated = i.DateCreated,
                DueDate = i.DueDate,
                DatePaid = i.DatePaid,
                Status = i.Status,
                Message = i.Message,
                PaymentTerms = i.PaymentTerms,
                FooterNote = i.FooterNote,
                DiscountValue = i.DiscountValue,
                DiscountType = i.DiscountType,
                DiscountBase = i.DiscountBase,
                DiscountScope = i.DiscountScope,
                TotalHT = i.TotalHT,
                TotalTVA = i.TotalTVA,
                TotalTTC = i.TotalTTC,
                AmountPaid = i.AmountPaid,
                PublicToken = i.PublicToken,
                Lines = i.Lines.OrderBy(l => l.Position).Select(l => new InvoiceLineDto
                {
                    Id = l.Id,
                    ProductId = l.ProductId,
                    Description = l.Description,
                    Date = l.Date,
                    Quantity = l.Quantity,
                    Unit = l.Unit,
                    UnitPrice = l.UnitPrice,
                    VATRate = l.VATRate,
                    TotalHT = l.TotalHT,
                    Type = l.Type,
                    Position = l.Position
                }).ToList(),
                History = i.History.OrderByDescending(h => h.Date).Select(h => new InvoiceHistoryDto 
                {
                    Date = h.Date,
                    Action = h.Action,
                    Details = h.Details,
                    UserName = h.UserName ?? "Système"
                }).ToList(),
                
                // Links
                QuoteId = i.QuoteId,
                QuoteReference = i.Quote?.Reference
            };
        }

        private async Task<string> GenerateInvoiceReferenceAsync()
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"FAC-{year}-";
            
            var last = await _context.Invoices
                .Where(q => q.Reference.StartsWith(prefix))
                .OrderByDescending(q => q.Reference)
                .FirstOrDefaultAsync();

            int next = 1;
            if (last != null)
            {
                 string[] parts = last.Reference.Split('-');
                 if (parts.Length == 3 && int.TryParse(parts[2], out int seq)) next = seq + 1;
            }
            return $"{prefix}{next.ToString("D3")}";
        }
        public async Task<InvoiceDto> DuplicateInvoiceAsync(Guid id)
        {
            var original = await _context.Invoices
                .Include(i => i.Lines)
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id);

            if (original == null) throw new Exception("Facture introuvable");

            var reference = await GenerateInvoiceReferenceAsync();
            
            var newInvoice = new Invoice
            {
                Reference = reference,
                ClientId = original.ClientId,
                DateCreated = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(30), // Or keep original term logic? defaulting to 30 or settings is safer.
                Status = InvoiceStatus.Draft,
                Message = original.Message,
                PaymentTerms = original.PaymentTerms,
                FooterNote = original.FooterNote,
                DiscountValue = original.DiscountValue,
                DiscountType = original.DiscountType,
                DiscountBase = original.DiscountBase,
                DiscountScope = original.DiscountScope,
                PublicToken = Guid.NewGuid(),
                TotalHT = original.TotalHT,
                TotalTVA = original.TotalTVA,
                TotalTTC = original.TotalTTC,
                Lines = original.Lines.Select(l => new InvoiceLine
                {
                    ProductId = l.ProductId,
                    Description = l.Description,
                    Date = null, // Reset date? Or keep? usually reset line date if service date. Let's keep null or today.
                    Quantity = l.Quantity,
                    Unit = l.Unit,
                    UnitPrice = l.UnitPrice,
                    VATRate = l.VATRate,
                    TotalHT = l.TotalHT,
                    Type = l.Type,
                    Position = l.Position
                }).ToList()
            };

            _context.Invoices.Add(newInvoice);
            await _context.SaveChangesAsync();
            return MapInvoiceToDto(newInvoice);
        }

        private async Task SyncQuoteToInvoiceAsync(Quote quote)
        {
            try
            {
                // 1. Find the target Invoice (NoTracking)
                var invoiceId = await _context.Invoices
                    .Where(i => i.QuoteId == quote.Id)
                    .Select(i => i.Id)
                    .FirstOrDefaultAsync();

                if (invoiceId == Guid.Empty) return;

                // Log source state
                try { System.IO.File.AppendAllText("sync_debug.txt", $"[{DateTime.Now}] START DirectSyncQuoteToInvoice: {quote.Reference} -> ID {invoiceId} (Lines: {quote.Lines.Count})\n"); } catch {}

                // 2. Clear tracker for this Invoice to prevent conflicts if it's already tracked
                var trackedInvoice = _context.Invoices.Local.FirstOrDefault(i => i.Id == invoiceId);
                if (trackedInvoice != null) _context.Entry(trackedInvoice).State = EntityState.Detached;

                // 3. Update Header directly in DB using ExecuteUpdate (bypasses tracker staleness)
                await _context.Invoices
                    .Where(i => i.Id == invoiceId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(i => i.ClientId, quote.ClientId)
                        .SetProperty(i => i.GuestName, quote.GuestName)
                        .SetProperty(i => i.GuestEmail, quote.GuestEmail)
                        .SetProperty(i => i.GuestPhone, quote.GuestPhone)
                        .SetProperty(i => i.Message, quote.Message)
                        .SetProperty(i => i.PaymentTerms, quote.PaymentTerms)
                        // .SetProperty(i => i.FooterNote, quote.FooterNote) // Dissociated as requested
                        .SetProperty(i => i.DiscountValue, quote.DiscountValue)
                        .SetProperty(i => i.DiscountType, quote.DiscountType)
                        .SetProperty(i => i.DiscountBase, quote.DiscountBase)
                        .SetProperty(i => i.DiscountScope, quote.DiscountScope)
                        .SetProperty(i => i.TotalHT, quote.TotalHT)
                        .SetProperty(i => i.TotalTVA, quote.TotalTVA)
                        .SetProperty(i => i.TotalTTC, quote.TotalTTC)
                    );

                // 4. Atomic Delete existing lines in DB
                await _context.InvoiceLines
                    .Where(l => l.InvoiceId == invoiceId)
                    .ExecuteDeleteAsync();

                // 5. Insert new lines
                foreach (var ql in quote.Lines)
                {
                    var newLine = new InvoiceLine
                    {
                        InvoiceId = invoiceId,
                        ProductId = ql.ProductId,
                        Description = ql.Description,
                        Date = ql.Date,
                        Quantity = ql.Quantity,
                        Unit = ql.Unit,
                        UnitPrice = ql.UnitPrice,
                        VATRate = ql.VATRate,
                        TotalHT = ql.TotalHT,
                        Type = ql.Type,
                        Position = ql.Position
                    };
                    _context.InvoiceLines.Add(newLine);
                }
                
                await _context.SaveChangesAsync();
                try { System.IO.File.AppendAllText("sync_debug.txt", $"[{DateTime.Now}] END DirectSyncQuoteToInvoice SUCCESS\n"); } catch {}
            }
            catch (Exception ex)
            {
                // Safety: Log but don't crash the main operation
                try { System.IO.File.AppendAllText("sync_error.txt", $"[{DateTime.Now}] SyncQuoteToInvoice FAILED: {ex}\n"); } catch {}
            }
        }

        private async Task SyncInvoiceToQuoteAsync(Invoice invoice)
        {
            if (!invoice.QuoteId.HasValue) return;
            var quoteId = invoice.QuoteId.Value;

            try
            {
                // 1. Find the target Quote (Check existence)
                var quoteExists = await _context.Quotes.AnyAsync(q => q.Id == quoteId);
                if (!quoteExists) return;

                // Log source state
                try { System.IO.File.AppendAllText("sync_debug.txt", $"[{DateTime.Now}] START DirectSyncInvoiceToQuote: {invoice.Reference} -> QuoteId {quoteId} (Lines: {invoice.Lines.Count})\n"); } catch {}

                // 2. Clear tracker for this Quote
                var trackedQuote = _context.Quotes.Local.FirstOrDefault(q => q.Id == quoteId);
                if (trackedQuote != null) _context.Entry(trackedQuote).State = EntityState.Detached;

                // 3. Update Header directly in DB using ExecuteUpdate
                await _context.Quotes
                    .Where(q => q.Id == quoteId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(q => q.ClientId, invoice.ClientId)
                        .SetProperty(q => q.GuestName, invoice.GuestName)
                        .SetProperty(q => q.GuestEmail, invoice.GuestEmail)
                        .SetProperty(q => q.GuestPhone, invoice.GuestPhone)
                        .SetProperty(q => q.Message, invoice.Message)
                        .SetProperty(q => q.PaymentTerms, invoice.PaymentTerms)
                        // .SetProperty(q => q.FooterNote, invoice.FooterNote) // Dissociated as requested
                        .SetProperty(q => q.DiscountValue, invoice.DiscountValue)
                        .SetProperty(q => q.DiscountType, invoice.DiscountType)
                        .SetProperty(q => q.DiscountBase, invoice.DiscountBase)
                        .SetProperty(q => q.DiscountScope, invoice.DiscountScope)
                        .SetProperty(q => q.TotalHT, invoice.TotalHT)
                        .SetProperty(q => q.TotalTVA, invoice.TotalTVA)
                        .SetProperty(q => q.TotalTTC, invoice.TotalTTC)
                    );

                // 4. Atomic Delete existing lines in DB
                await _context.QuoteLines
                    .Where(l => l.QuoteId == quoteId)
                    .ExecuteDeleteAsync();

                // 5. Insert new lines
                foreach (var il in invoice.Lines)
                {
                    var newLine = new QuoteLine
                    {
                        QuoteId = quoteId,
                        ProductId = il.ProductId,
                        Description = il.Description,
                        Date = il.Date,
                        Quantity = il.Quantity,
                        Unit = il.Unit,
                        UnitPrice = il.UnitPrice,
                        VATRate = il.VATRate,
                        TotalHT = il.TotalHT,
                        Type = il.Type,
                        Position = il.Position
                    };
                    _context.QuoteLines.Add(newLine);
                }
                
                await _context.SaveChangesAsync();
                try { System.IO.File.AppendAllText("sync_debug.txt", $"[{DateTime.Now}] END DirectSyncInvoiceToQuote SUCCESS\n"); } catch {}
            }
            catch (Exception ex)
            {
                // Safety: Log but don't crash the main operation
                try { System.IO.File.AppendAllText("sync_error.txt", $"[{DateTime.Now}] SyncInvoiceToQuote FAILED: {ex}\n"); } catch {}
            }
        }
    }
}
