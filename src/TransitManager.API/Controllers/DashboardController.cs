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
        private readonly IVehiculeService _vehiculeService;
        private readonly IColisService _colisService;
        private readonly IDocumentService _documentService;
        private readonly IClientService _clientService;
        private readonly IPaiementService _paiementService;

        public DashboardController(
            IColisService colisService,
            IDocumentService documentService,
            IClientService clientService,
            IPaiementService paiementService,
            IVehiculeService vehiculeService)
        {
            _colisService = colisService;
            _documentService = documentService;
            _clientService = clientService;
            _paiementService = paiementService;
            _vehiculeService = vehiculeService;
        }

        [HttpGet("admin")]
        public async Task<ActionResult<AdminDashboardStatsDto>> GetAdminStats()
        {
            var stats = new AdminDashboardStatsDto();

            // KPIs
            // On considère "En Transit" comme le statut principal, mais on pourrait sommer.
            // MODIFICATION: User requested "Delayed Items > 5 days" instead of just "En Transit".
            // However, the DTO field is still named ColisEnTransit. Ideally we rename it in DTO, but to avoid breaking frontend now, 
            // we can reuse existing field or just frontend will interpret it.
            // Better: stick to API being consistent, Frontend changes label. 
            // Or better: Let's count the delayed items here so the number on the card is correct.
            
            var delayedColis = await _colisService.GetColisWaitingLongTimeAsync(5);
            var delayedVehi = await _vehiculeService.GetDelayedVehiculesAsync(5);
            
            // We use the field ColisEnTransit to store this count for now (repurposing) 
            // OR we rely on the DTO having only specific fields. 
            // Let's assume frontend will use this value locally or fetch it.
            // Wait, the Home.razor uses `stats.ColisEnTransit`. I should update this value to be the count of delayed items.
            stats.ColisEnTransit = delayedColis.Count() + delayedVehi.Count();
            
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

        [HttpGet("admin/delayed-items")]
        public async Task<List<DashboardEntityDto>> GetDelayedItems()
        {
            var delayedColis = await _colisService.GetColisWaitingLongTimeAsync(5); // Filtered by Conteneur=null in Repo? Yes, checked.
            var delayedVehicules = await _vehiculeService.GetDelayedVehiculesAsync(5);

            var list = new List<DashboardEntityDto>();
            
            list.AddRange(delayedColis.Select(c => new DashboardEntityDto
            {
                Id = c.Id,
                Type = "Colis",
                Reference = c.NumeroReference,
                Description = c.Designation,
                ClientName = c.Client?.NomComplet ?? "Inconnu",
                DateCreation = c.DateCreation, // Actually DateArrivee is usually used for Colis as "Creation" date in this system? Repo uses DateArrivee.
                DaysDelay = (DateTime.UtcNow - c.DateArrivee).Days,
                Status = c.Statut.ToString()
            }));

            list.AddRange(delayedVehicules.Select(v => new DashboardEntityDto
            {
                Id = v.Id,
                Type = "Vehicule",
                Reference = v.Immatriculation,
                Description = $"{v.Marque} {v.Modele}",
                ClientName = v.Client?.NomComplet ?? "Inconnu",
                DateCreation = v.DateCreation,
                DaysDelay = (DateTime.UtcNow - v.DateCreation).Days,
                Status = v.Statut.ToString()
            }));

            return list.OrderByDescending(x => x.DaysDelay).ToList();
        }

        [HttpGet("admin/new-clients")]
        public async Task<List<Client>> GetNewClientsList()
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var clients = await _clientService.GetNewClientsListAsync(startOfMonth);
            return clients.ToList();
        }

        [HttpGet("admin/unpriced-items")]
        public async Task<List<DashboardEntityDto>> GetUnpricedItems()
        {
            var colis = await _colisService.GetUnpricedColisAsync();
            var vehicules = await _vehiculeService.GetUnpricedVehiculesAsync();

            var list = new List<DashboardEntityDto>();

            list.AddRange(colis.Select(c => new DashboardEntityDto
            {
                Id = c.Id,
                Type = "Colis",
                Reference = c.NumeroReference,
                Description = c.Designation,
                ClientName = c.Client?.NomComplet ?? "Inconnu",
                DateCreation = c.DateCreation,
                DaysDelay = 0, // Not relevant here
                Status = c.Statut.ToString()
            }));

            list.AddRange(vehicules.Select(v => new DashboardEntityDto
            {
                Id = v.Id,
                Type = "Vehicule",
                Reference = v.Immatriculation,
                Description = $"{v.Marque} {v.Modele}",
                ClientName = v.Client?.NomComplet ?? "Inconnu",
                DateCreation = v.DateCreation,
                DaysDelay = 0,
                Status = v.Statut.ToString()
            }));

            return list.OrderByDescending(x => x.DateCreation).ToList();
        }
    }
}
