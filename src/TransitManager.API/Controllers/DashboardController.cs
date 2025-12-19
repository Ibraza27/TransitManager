using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using TransitManager.Core.DTOs;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Core.Entities;

namespace TransitManager.API.Controllers
{
    [Authorize(Roles = "Administrateur,Gestionnaire")]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IColisService _colisService;
        private readonly IDocumentService _documentService;
        private readonly IClientService _clientService;
        private readonly IPaiementService _paiementService;

        public DashboardController(
            IColisService colisService,
            IDocumentService documentService,
            IClientService clientService,
            IPaiementService paiementService)
        {
            _colisService = colisService;
            _documentService = documentService;
            _clientService = clientService;
            _paiementService = paiementService;
        }

        [HttpGet("admin")]
        public async Task<ActionResult<AdminDashboardStatsDto>> GetAdminStats()
        {
            var stats = new AdminDashboardStatsDto();

            // KPIs
            // On considère "En Transit" comme le statut principal, mais on pourrait sommer.
            stats.ColisEnTransit = await _colisService.GetCountByStatusAsync(StatutColis.EnTransit); 
            
            stats.DocsAValider = await _documentService.GetPendingDocumentsCountAsync();
            stats.MissingDocumentsCount = await _documentService.GetTotalMissingDocumentsCountAsync();
            
            
            // Clients créés depuis le début du mois courant
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            stats.NouveauxClientsMois = await _clientService.GetNewClientsCountAsync(startOfMonth);
            
            // Volume de ce mois
            var volumes = await _colisService.GetMonthlyVolumeAsync(1);
            stats.VolumeMensuel = volumes.Values.FirstOrDefault();

            // Charts History
            var revenue = await _paiementService.GetMonthlyRevenueHistoryAsync(6);
            stats.RevenueLast6Months = revenue.Select(k => new MonthlyMetricDto { Month = k.Key, Value = k.Value }).ToList();
            
            var volumeHist = await _colisService.GetMonthlyVolumeAsync(6);
            stats.VolumeLast6Months = volumeHist.Select(k => new MonthlyMetricDto { Month = k.Key, Value = k.Value }).ToList();

            // Status Distribution
            var statusStats = await _colisService.GetStatisticsByStatusAsync();
            stats.StatusDistribution = statusStats.ToDictionary(k => k.Key.ToString(), v => v.Value);

            return Ok(stats);
        }

        [HttpGet("admin/missing-documents")]
        public async Task<ActionResult<IEnumerable<Document>>> GetAdminMissingDocuments()
        {
            var docs = await _documentService.GetAllMissingDocumentsAsync();
            return Ok(docs);
        }
    }
}
