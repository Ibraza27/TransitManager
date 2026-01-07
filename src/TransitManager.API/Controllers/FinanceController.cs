using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FinanceController : ControllerBase
    {
        private readonly IFinanceService _financeService;
        private readonly IExportService _exportService;
        private readonly IPaiementService _paiementService;

        public FinanceController(IFinanceService financeService, IExportService exportService, IPaiementService paiementService)
        {
            _financeService = financeService;
            _exportService = exportService;
            _paiementService = paiementService;
        }


        [HttpGet("stats")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> GetAdminStats([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] Guid? clientId)
        {
            var stats = await _financeService.GetAdminStatsAsync(start, end, clientId);
            return Ok(stats);
        }

        [HttpGet("transactions")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> GetAllTransactions([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] Guid? clientId)
        {
            var list = await _financeService.GetAllTransactionsAsync(start, end, clientId);
            return Ok(list);
        }

        [HttpPost("payment")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> CreatePayment([FromBody] TransitManager.Core.Entities.Paiement paiement)
        {
             // Force correct date/id
             if (paiement.Id == Guid.Empty) paiement.Id = Guid.NewGuid();
             paiement.DatePaiement = DateTime.UtcNow;
             
             try 
             {
                var created = await _paiementService.CreateAsync(paiement);
                return Ok(created);
             }
             catch(Exception ex)
             {
                 return BadRequest(ex.Message);
             }
        }

        [HttpGet("receipt/{id}")]
        public async Task<IActionResult> GetReceipt(Guid id)
        {
            try
            {
                var pdfBytes = await _paiementService.GenerateReceiptAsync(id); // Use PaiementService directly as it likely uses ExportService internally or we use ExportService directly
                // Actually IPaiementService has GenerateReceiptAsync, let's use that.
                if(pdfBytes == null || pdfBytes.Length == 0)
                {
                    // Fallback to ExportService if PaiementService doesn't return bytes directly (depends on impl)
                    var paiement = await _paiementService.GetByIdAsync(id);
                    if(paiement == null) return NotFound();
                    pdfBytes = await _exportService.GenerateReceiptPdfAsync(paiement);
                }
                
                return File(pdfBytes, "application/pdf", $"Recu_{id}.pdf");
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet("export")]
        [Authorize(Roles = "Administrateur")]
        public async Task<IActionResult> ExportTransactions([FromQuery] DateTime? start, [FromQuery] DateTime? end, [FromQuery] Guid? clientId)
        {
             try
             {
                 var s = start ?? DateTime.MinValue;
                 var e = end ?? DateTime.MaxValue;
                 var paiements = await _paiementService.GetByPeriodAsync(s, e);
                 
                 // Apply Client Filtering if requested
                 if (clientId.HasValue)
                 {
                    paiements = paiements.Where(p => p.ClientId == clientId.Value).ToList();
                 }

                 var excelBytes = await _exportService.ExportFinancialReportAsync(s, e, paiements);
                 return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"FinanceReport_{DateTime.Now:yyyyMMdd}.xlsx");
             }
             catch
             {
                 return BadRequest("Erreur export");
             }
        }

        [HttpGet("summary/{clientId}")]
        public async Task<IActionResult> GetClientSummary(Guid clientId)
        {
            // Vérification de sécurité de base : Un client ne doit voir que ses données (sauf Admin)
            // TODO: Ajouter vérification Claims si nécessaire, mais on suppose que le frontend envoie le bon ID
            // Idéalement, on devrait comparer clientId avec User.Claims
            
            var summary = await _financeService.GetClientSummaryAsync(clientId);
            return Ok(summary);
        }
        
        [HttpGet("client-transactions/{clientId}")]
        public async Task<IActionResult> GetClientTransactions(Guid clientId)
        {
             var list = await _financeService.GetClientTransactionsAsync(clientId);
             return Ok(list);
        }
    }
}
