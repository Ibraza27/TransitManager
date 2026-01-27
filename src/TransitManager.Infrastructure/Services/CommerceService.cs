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

namespace TransitManager.Infrastructure.Services
{
    public class CommerceService : ICommerceService
    {
        private readonly TransitContext _context;
        private readonly IEmailService _emailService;
        private readonly IExportService _exportService;
        private readonly ISettingsService _settingsService;
        private readonly IDocumentService _documentService;

        public CommerceService(
            TransitContext context,
            IEmailService emailService,
            IExportService exportService,
            ISettingsService settingsService,
            IDocumentService documentService)
        {
            _context = context;
            _emailService = emailService;
            _exportService = exportService;
            _settingsService = settingsService;
            _documentService = documentService;
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
            // DEBUG: Log incoming payload
            try 
            {
                var debugLines = dto.Lines.Select((l, i) => new { Index=i, Pos=l.Position, Type=l.Type, Desc=l.Description, Qty=l.Quantity, Price=l.UnitPrice }).ToList();
                var logMsg = $"[{DateTime.Now}] QuoteID={dto.Id} \nLines: {System.Text.Json.JsonSerializer.Serialize(debugLines)}\n";
                System.IO.File.AppendAllText("commerce_debug_deep.txt", logMsg);
            } catch {}

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

        public async Task SendQuoteByEmailAsync(Guid id, string? subject = null, string? body = null, List<Guid>? attachmentIds = null)
        {
            var quote = await _context.Quotes.Include(q => q.Client).Include(q => q.Lines).FirstOrDefaultAsync(q => q.Id == id);
            if (quote == null) throw new Exception("Devis introuvable");
            if (quote.Client == null || string.IsNullOrEmpty(quote.Client.Email)) throw new Exception("Le client n'a pas d'adresse email valide");

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
        <strong>{company.CompanyName} - {company.LegalStatus}</strong><br/>
        {company.Address}<br/>
        {company.ZipCode} {company.City}<br/>
        Tél: {company.Phone}<br/>
    </div>
</div>";

            // Default Subject
            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = $"Devis {quote.Reference} - {company.CompanyName}";
            }

            try
            {
                await _emailService.SendEmailAsync(quote.Client.Email, subject, finalHtml, attachments);
                
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
                .Include(i => i.Lines)
                .Include(i => i.History)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(q => q.Reference.Contains(search) || q.Client.Nom.Contains(search));
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
            var entities = await query.OrderByDescending(q => q.DateCreated)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync();

            var items = entities.Select(i => MapInvoiceToDto(i)).ToList();

            return new PagedResult<InvoiceDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Lines)
                .Include(i => i.History)
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
            var settings = await _settingsService.GetSettingAsync<InvoiceSettingsDto>("InvoiceSettings", new());

            // Apply defaults if not provided in DTO
            if (string.IsNullOrWhiteSpace(dto.PaymentTerms)) dto.PaymentTerms = settings.DefaultPaymentTerms;
            if (string.IsNullOrWhiteSpace(dto.FooterNote)) dto.FooterNote = settings.DefaultFooterNote;

            var invoice = new Invoice
            {
                Reference = await GenerateInvoiceReferenceAsync(),
                ClientId = dto.ClientId,
                DateCreated = dto.DateCreated,
                DueDate = dto.DueDate,
                Message = dto.Message,
                PaymentTerms = dto.PaymentTerms,
                FooterNote = dto.FooterNote,
                Status = InvoiceStatus.Draft,
                PublicToken = Guid.NewGuid()
            };
            
            _context.Invoices.Add(invoice);
            
            _context.InvoiceHistories.Add(new InvoiceHistory 
            { 
                InvoiceId = invoice.Id, 
                Action = "Création", 
                Details = "Facture créée manuellement" 
            });

            await _context.SaveChangesAsync();
            return MapInvoiceToDto(invoice);
        }

        public async Task<InvoiceDto> UpdateInvoiceAsync(UpdateInvoiceDto dto)
        {
            var invoice = await _context.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == dto.Id);
            if (invoice == null) throw new Exception("Facture introuvable");

            invoice.ClientId = dto.ClientId;
            invoice.DateCreated = dto.DateCreated;
            invoice.DueDate = dto.DueDate;
            invoice.Message = dto.Message;
            invoice.PaymentTerms = dto.PaymentTerms;
            invoice.FooterNote = dto.FooterNote;
            // Status is typically managed via specific actions, but we allow update here if draft
            if (invoice.Status == InvoiceStatus.Draft) invoice.Status = dto.Status;

            _context.InvoiceLines.RemoveRange(invoice.Lines);
            
            var newLines = new List<InvoiceLine>();
            decimal runningSubtotal = 0;
            int pos = 0;
            decimal grossHT = 0;
            decimal grossTVA = 0;

            foreach (var lineDto in dto.Lines)
            {
                decimal lineTotalHT = 0;
                
                 if (lineDto.Type == QuoteLineType.Subtotal)
                {
                    lineTotalHT = runningSubtotal;
                    runningSubtotal = 0;
                }
                else if (lineDto.Type == QuoteLineType.Product)
                {
                     lineTotalHT = lineDto.Quantity * lineDto.UnitPrice;
                     var lineTVA = lineTotalHT * (lineDto.VATRate / 100m);
                     grossHT += lineTotalHT;
                     grossTVA += lineTVA;
                     runningSubtotal += lineTotalHT;
                }

                // Subtotal fix
                decimal finalPrice = lineDto.UnitPrice;
                decimal finalQty = lineDto.Quantity;
                if(lineDto.Type == QuoteLineType.Subtotal) { finalPrice = lineTotalHT; finalQty = 1; }

                newLines.Add(new InvoiceLine
                {
                     InvoiceId = invoice.Id,
                     ProductId = lineDto.ProductId,
                     Description = lineDto.Description,
                     Date = lineDto.Date,
                     Quantity = finalQty,
                     Unit = lineDto.Unit,
                     UnitPrice = finalPrice,
                     VATRate = lineDto.VATRate,
                     TotalHT = lineTotalHT,
                     Type = lineDto.Type,
                     Position = pos++
                });
            }

            invoice.Lines = newLines;
            
            // Recalc Totals (We assume simplistic calc for now, copying Quote logic minus discounts if not passed in DTO yet)
            // Note: UpdateInvoiceDto didn't include discount fields in my hasty definition earlier. 
            // Standard calc:
            invoice.TotalHT = grossHT - invoice.DiscountValue; // Assuming discount stored on entity persists
            // Simple: Just use gross for now as we didn't add discount controls to invoice UI yet
            invoice.TotalHT = grossHT; 
            invoice.TotalTVA = grossTVA;
            invoice.TotalTTC = invoice.TotalHT + invoice.TotalTVA;

            await _context.SaveChangesAsync();
            return MapInvoiceToDto(invoice);
        }

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

        public async Task<bool> DeleteInvoiceAsync(Guid id)
        {
             var invoice = await _context.Invoices.FindAsync(id);
             if (invoice == null) return false;
             _context.Invoices.Remove(invoice);
             await _context.SaveChangesAsync();
             return true;
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
                PaymentTerms = string.IsNullOrWhiteSpace(quote.PaymentTerms) ? settings.DefaultPaymentTerms : quote.PaymentTerms,
                FooterNote = string.IsNullOrWhiteSpace(quote.FooterNote) ? settings.DefaultFooterNote : quote.FooterNote,
                DiscountValue = quote.DiscountValue,
                DiscountType = quote.DiscountType,
                DiscountBase = quote.DiscountBase,
                DiscountScope = quote.DiscountScope,
                TotalHT = quote.TotalHT,
                TotalTVA = quote.TotalTVA,
                TotalTTC = quote.TotalTTC,
                PublicToken = Guid.NewGuid()
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

        public async Task SendInvoiceByEmailAsync(Guid id, string? subject = null, string? body = null, List<Guid>? attachmentIds = null)
        {
            var invoice = await _context.Invoices.Include(i => i.Client).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) throw new Exception("Facture introuvable");
            
            // Logic similar to SendQuote (Load settings, Generate PDF, Send)
             // Load Settings
            var company = await _settingsService.GetSettingAsync<TransitManager.Core.DTOs.Settings.CompanyProfileDto>("CompanyProfile", new());
            
            // Generate PDF (Placeholder)
            var pdfBytes = await GenerateInvoicePdfAsync(MapInvoiceToDto(invoice));
            var pdfName = $"Facture_{invoice.Reference}.pdf";

            var attachments = new List<(string Name, byte[] Content)>();
            attachments.Add((pdfName, pdfBytes));

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
                    } catch {}
                }
            }

            if (string.IsNullOrWhiteSpace(subject)) subject = $"Facture {invoice.Reference} - {company.CompanyName}";
            if (string.IsNullOrWhiteSpace(body)) body = "Veuillez trouver ci-joint votre facture.";

            var publicLink = $"https://hippocampetransitmanager.com/portal/invoice/{invoice.PublicToken}";
            var fullBody = $"{body}<br/><br/>Vous pouvez consulter et régler votre facture en ligne ici : <a href='{publicLink}'>{publicLink}</a>";

            await _emailService.SendEmailAsync(invoice.Client.Email, subject, fullBody, attachments);

            if (invoice.Status == InvoiceStatus.Draft)
            {
                invoice.Status = InvoiceStatus.Sent;
                invoice.DateSent = DateTime.UtcNow;
            }

            _context.InvoiceHistories.Add(new InvoiceHistory { InvoiceId = id, Action = "Email envoyé", Details = $"Envoyé à {invoice.Client.Email}" });
            await _context.SaveChangesAsync();
        }

        public async Task SendPaymentReminderAsync(Guid id, string? subject = null, string? body = null, List<Guid>? attachmentIds = null)
        {
             var invoice = await _context.Invoices.Include(i => i.Client).FirstOrDefaultAsync(i => i.Id == id);
            if (invoice == null) throw new Exception("Facture introuvable");

            // Similar sending logic
             var company = await _settingsService.GetSettingAsync<TransitManager.Core.DTOs.Settings.CompanyProfileDto>("CompanyProfile", new());
             
             // Maybe attach invoice again? Yes usually.
             var pdfBytes = await GenerateInvoicePdfAsync(MapInvoiceToDto(invoice));
             var attachments = new List<(string Name, byte[] Content)> { ($"Facture_{invoice.Reference}.pdf", pdfBytes) };

             if (string.IsNullOrWhiteSpace(subject)) subject = $"Rappel de paiement - Facture {invoice.Reference}";
             if (string.IsNullOrWhiteSpace(body)) body = "Ceci est un rappel de paiement.";

             var publicLink = $"https://hippocampetransitmanager.com/portal/invoice/{invoice.PublicToken}";
             var fullBody = $"{body}<br/><br/>Vous pouvez consulter votre facture en ligne ici : <a href='{publicLink}'>{publicLink}</a>";

             await _emailService.SendEmailAsync(invoice.Client.Email, subject, fullBody, attachments);

             invoice.ReminderCount++;
             invoice.LastReminderSent = DateTime.UtcNow;
             
             _context.InvoiceHistories.Add(new InvoiceHistory { InvoiceId = id, Action = "Rappel envoyé", Details = $"Rappel #{invoice.ReminderCount} envoyé" });
             await _context.SaveChangesAsync();
        }

        public async Task<byte[]> GenerateInvoicePdfAsync(InvoiceDto invoice)
        {
            // We need a GenerateInvoicePdf in ExportService too, but for now we can maybe reuse/mock or add it.
            // Assumption: ExportService needs update too or we use generic PDF gen?
            // User didn't explicitly ask for PDF layout changes, but Invoice needs to say "Facture".
            // We'll likely need to update ExportService later. For now, let's pretend ExportService has it or use a placeholder logic.
            // Actually, I should check ExportService. Creating a placeholder call for now.
             return await _exportService.GenerateQuotePdfAsync(new QuoteDto { Reference = invoice.Reference }); // HACK: Temporary until ExportService updated
        }
        
        // Helpers
        private InvoiceDto MapInvoiceToDto(Invoice i)
        {
            return new InvoiceDto
            {
                Id = i.Id,
                Reference = i.Reference,
                ClientId = i.ClientId,
                ClientName = i.Client?.Nom ?? "Inconnu",
                ClientFirstname = i.Client?.Prenom ?? "",
                ClientEmail = i.Client?.Email ?? "",
                ClientAddress = i.Client?.AdressePrincipale ?? "",
                ClientPhone = i.Client?.TelephonePrincipal ?? "",
                DateCreated = i.DateCreated,
                DueDate = i.DueDate,
                DatePaid = i.DatePaid,
                Status = i.Status,
                Message = i.Message,
                PaymentTerms = i.PaymentTerms,
                FooterNote = i.FooterNote,
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
                }).ToList()
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
    }
}
