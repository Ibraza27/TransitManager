using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace TransitManager.Infrastructure.Services
{
    public class FinanceService : IFinanceService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly Microsoft.Extensions.Logging.ILogger<FinanceService> _logger;

        public FinanceService(IDbContextFactory<TransitContext> contextFactory, Microsoft.Extensions.Logging.ILogger<FinanceService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task<FinanceStatsDto> GetAdminStatsAsync(DateTime? startDate = null, DateTime? endDate = null, Guid? clientId = null)
        {
            try 
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                var now = DateTime.UtcNow;
                
                // Determine ranges
                var actualStart = startDate.HasValue ? DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc) : new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var actualEnd = endDate.HasValue ? DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc) : DateTime.UtcNow;
                
                var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                _logger.LogInformation($"GetAdminStatsAsync: ClientId={clientId}");

                var query = context.Paiements.AsQueryable();
                if (clientId.HasValue) query = query.Where(p => p.ClientId == clientId.Value);

                // 1. Chiffre d'affaires (Filtered by Date Range provided or Current Month if null)
                // Note: User asked for "CA du mois" context primarily, but if date range changes, it should reflect range.
                // Let's interpret: 
                // - CA Mensuel (box) -> Revenue in the selected period? Or strict "Current Month"?
                // usually dashboards show "Period Revenue" if filtered.
                // Let's use the provided range for the main "Monthly" box or rename it conceptually to "Period Revenue".
                // However, to keep it simple, if filters are present, we calculate based on them.
                
                var paymentsInPeriod = query.Where(p => p.DatePaiement >= actualStart && p.DatePaiement <= actualEnd);
                var caMensuel = await paymentsInPeriod.SumAsync(p => p.Montant);
                
                var paymentsYear = query.Where(p => p.DatePaiement >= startOfYear);
                var caAnnuel = await paymentsYear.SumAsync(p => p.Montant);

                var totalEncaisse = await query.SumAsync(p => p.Montant);

                // 2. Reste à Payer (Filter by Client if selected)
                var colisQuery = context.Colis.Where(c => c.Actif);
                var vehiculeQuery = context.Vehicules.Where(v => v.Actif);

                if (clientId.HasValue)
                {
                    colisQuery = colisQuery.Where(c => c.ClientId == clientId.Value);
                    vehiculeQuery = vehiculeQuery.Where(v => v.ClientId == clientId.Value);
                }

                var colisDette = await colisQuery.SumAsync(c => (c.TypeEnvoi == TypeEnvoi.AvecDedouanement ? c.PrixTotal + c.FraisDouane : c.PrixTotal) - c.SommePayee);
                var vehiculeDette = await vehiculeQuery.SumAsync(v => v.PrixTotal - v.SommePayee);

                // 3. Articles sans prix (Cotation Requise)
                // Remplace "Paiements en retard" par "Articles sans prix"
                var unpricedColis = await colisQuery.CountAsync(c => c.PrixTotal == 0);
                var unpricedVehicules = await vehiculeQuery.CountAsync(v => v.PrixTotal == 0);
                var unpricedCount = unpricedColis + unpricedVehicules;

                // 4. Graphique
                // If filter is applied, graph should perhaps show that range? 
                // Or stick to 12 months for the selected client. User said "avoir les donnée du graphique".
                // Let's stick to last 12 months but filtered by client.
                
                var twelveMonthsAgo = now.AddMonths(-11).Date;
                var startOfPeriod = new DateTime(twelveMonthsAgo.Year, twelveMonthsAgo.Month, 1, 0, 0, 0, DateTimeKind.Utc);

                var chartQuery = context.Paiements.Where(p => p.DatePaiement >= startOfPeriod);
                if (clientId.HasValue) chartQuery = chartQuery.Where(p => p.ClientId == clientId.Value);

                var monthlyStats = await chartQuery
                    .GroupBy(p => new { p.DatePaiement.Year, p.DatePaiement.Month })
                    .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(p => p.Montant) })
                    .ToListAsync();

                var chartData = new List<MonthlyRevenueDto>();
                for (int i = 11; i >= 0; i--)
                {
                    var targetDate = now.AddMonths(-i);
                    var stat = monthlyStats.FirstOrDefault(s => s.Year == targetDate.Year && s.Month == targetDate.Month);
                    
                    chartData.Add(new MonthlyRevenueDto 
                    { 
                        MonthLabel = targetDate.ToString("MMM yy"), 
                        Revenue = stat?.Total ?? 0 
                    });
                }
                
                return new FinanceStatsDto
                {
                    ChiffreAffairesMensuel = caMensuel,
                    ChiffreAffairesAnnuel = caAnnuel,
                    TotalEncaisse = totalEncaisse,
                    TotalRestantDu = colisDette + vehiculeDette,
                    NombrePaiementsRetard = unpricedCount, // Mapped to Unpriced Items
                    RevenueChartData = chartData
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAdminStatsAsync");
                throw;
            }
        }

        public async Task<IEnumerable<FinancialTransactionDto>> GetAllTransactionsAsync(DateTime? startDate, DateTime? endDate, Guid? clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Paiements.AsNoTracking().AsQueryable();

            if (startDate.HasValue) 
            {
                var utcStart = startDate.Value.Kind == DateTimeKind.Utc ? startDate.Value : DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
                query = query.Where(p => p.DatePaiement >= utcStart);
            }
            if (endDate.HasValue) 
            {
                var utcEnd = endDate.Value.Kind == DateTimeKind.Utc ? endDate.Value : DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
                query = query.Where(p => p.DatePaiement <= utcEnd);
            }
            if (clientId.HasValue) query = query.Where(p => p.ClientId == clientId.Value);

            // Projection directe pour éviter les cycles et charger inutilement les entités
            var dtos = await query
                .OrderByDescending(p => p.DatePaiement)
                .Select(p => new FinancialTransactionDto
                {
                    Id = p.Id,
                    Date = p.DatePaiement,
                    ReferenceRecu = p.NumeroRecu,
                    ClientName = p.Client != null ? p.Client.Prenom + " " + p.Client.Nom : "Inconnu",
                    EntityType = p.ColisId.HasValue ? "Colis" : (p.VehiculeId.HasValue ? "Vehicule" : (p.ConteneurId.HasValue ? "Conteneur" : "Autre")),
                    EntityReference = p.ColisId.HasValue ? (p.Colis != null ? p.Colis.NumeroReference : "Introuvable") :
                                      (p.VehiculeId.HasValue ?(p.Vehicule != null ? p.Vehicule.Immatriculation : "Introuvable") :
                                      (p.ConteneurId.HasValue ? (p.Conteneur != null ? p.Conteneur.NumeroDossier : "Introuvable") : "-")),
                    EntityId = p.ColisId ?? p.VehiculeId ?? p.ConteneurId,
                    Montant = p.Montant,
                    ModePaiement = p.ModePaiement.ToString(),
                    Statut = p.Statut.ToString()
                })
                .ToListAsync();

            return dtos;
        }

        public async Task<ClientFinanceSummaryDto> GetClientSummaryAsync(Guid clientId)
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

                // 1. Total Payé
                var totalPaye = await context.Paiements
                    .Where(p => p.ClientId == clientId && p.Statut == StatutPaiement.Valide)
                    .SumAsync(p => p.Montant);

                // 2. Impayés (Colis + Véhicules)
                var unpaidItems = new List<UnpaidItemDto>();

                var colisImpayes = await context.Colis
                    .Where(c => c.ClientId == clientId && c.Actif && 
                        ((c.TypeEnvoi == TypeEnvoi.AvecDedouanement ? c.PrixTotal + c.FraisDouane : c.PrixTotal) - c.SommePayee) > 0.01m)
                    .Select(c => new UnpaidItemDto
                    {
                        EntityId = c.Id,
                        EntityType = "Colis",
                        Reference = c.NumeroReference,
                        Description = c.Designation,
                        MontantTotal = c.TypeEnvoi == TypeEnvoi.AvecDedouanement ? c.PrixTotal + c.FraisDouane : c.PrixTotal,
                        RestantAPayer = (c.TypeEnvoi == TypeEnvoi.AvecDedouanement ? c.PrixTotal + c.FraisDouane : c.PrixTotal) - c.SommePayee,
                        DateCreation = c.DateCreation
                    })
                    .ToListAsync();

                var vehiculesImpayes = await context.Vehicules
                    .Where(v => v.ClientId == clientId && v.Actif && (v.PrixTotal - v.SommePayee) > 0.01m)
                    .Select(v => new UnpaidItemDto
                    {
                        EntityId = v.Id,
                        EntityType = "Vehicule",
                        Reference = v.Immatriculation,
                        Description = v.Marque + " " + v.Modele,
                        MontantTotal = v.PrixTotal,
                        RestantAPayer = v.PrixTotal - v.SommePayee,
                        DateCreation = v.DateCreation
                    })
                    .ToListAsync();

                unpaidItems.AddRange(colisImpayes);
                unpaidItems.AddRange(vehiculesImpayes);

                // 3. Derniers paiements (Projection)
                var recentPaiements = await context.Paiements
                    .Where(p => p.ClientId == clientId)
                    .OrderByDescending(p => p.DatePaiement)
                    .Take(10)
                    .Select(p => new FinancialTransactionDto
                    {
                        Id = p.Id,
                        Date = p.DatePaiement,
                        ReferenceRecu = p.NumeroRecu,
                        ClientName = p.Client != null ? p.Client.Prenom + " " + p.Client.Nom : "Moi",
                        // Safely handle null navigation properties using null checks
                        EntityType = p.ColisId != null ? "Colis" : (p.VehiculeId != null ? "Vehicule" : "Autre"),
                        EntityReference = p.ColisId != null ? (p.Colis != null ? p.Colis.NumeroReference : "Ref. " + p.ColisId.ToString().Substring(0,8)) :
                                          (p.VehiculeId != null ? (p.Vehicule != null ? p.Vehicule.Immatriculation : "Imm. " + p.VehiculeId.ToString().Substring(0,8)) : ""),
                        EntityId = p.ColisId ?? p.VehiculeId,
                        Montant = p.Montant,
                        ModePaiement = p.ModePaiement.ToString(),
                        Statut = p.Statut.ToString()
                    })
                    .ToListAsync();

                return new ClientFinanceSummaryDto
                {
                    TotalPayeHistorique = totalPaye,
                    SoldeTotalAPayer = unpaidItems.Sum(x => x.RestantAPayer),
                    RestantAPayerColis = unpaidItems.Where(x => x.EntityType == "Colis").Sum(x => x.RestantAPayer),
                    RestantAPayerVehicule = unpaidItems.Where(x => x.EntityType == "Vehicule").Sum(x => x.RestantAPayer),
                    Impayes = unpaidItems.OrderByDescending(x => x.DateCreation).ToList(),
                    DerniersPaiements = recentPaiements
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetClientSummaryAsync for client {clientId}");
                throw;
            }
        }

        public async Task<IEnumerable<FinancialTransactionDto>> GetClientTransactionsAsync(Guid clientId)
        {
            return await GetAllTransactionsAsync(null, null, clientId);
        }

        // MapToDto inutilisé maintenant car on fait des projections, mais on peut le garder ou supprimer
        // Supprimé pour clarté
    }
}
