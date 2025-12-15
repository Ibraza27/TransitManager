using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Repositories
{
    // 1. Définition de l'interface
    public interface IPaiementRepository : IGenericRepository<Paiement>
    {
        Task<IEnumerable<Paiement>> GetByColisAsync(Guid colisId);
        Task<IEnumerable<Paiement>> GetByVehiculeAsync(Guid vehiculeId);
        Task<IEnumerable<Paiement>> GetByClientAsync(Guid clientId);
        Task<IEnumerable<Paiement>> GetByConteneurAsync(Guid conteneurId);
        Task<IEnumerable<Paiement>> GetByPeriodAsync(DateTime debut, DateTime fin);
        Task<IEnumerable<Paiement>> GetOverduePaymentsAsync();
        Task<decimal> GetMonthlyRevenueAsync(DateTime month);
    }

    // 2. Implémentation de la classe
    public class PaiementRepository : GenericRepository<Paiement>, IPaiementRepository
    {
        // On hérite le _context du GenericRepository

        public PaiementRepository(TransitContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Paiement>> GetByColisAsync(Guid colisId)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Where(p => p.ColisId == colisId && p.Actif)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByVehiculeAsync(Guid vehiculeId)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Where(p => p.VehiculeId == vehiculeId && p.Actif)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByClientAsync(Guid clientId)
        {
            return await _context.Paiements
                .Include(p => p.Conteneur)
                .Where(p => p.ClientId == clientId && p.Actif)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByConteneurAsync(Guid conteneurId)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Where(p => p.ConteneurId == conteneurId && p.Actif)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByPeriodAsync(DateTime debut, DateTime fin)
        {
            // Conversion UTC pour être sûr
            var debutUtc = debut.ToUniversalTime();
            var finUtc = fin.ToUniversalTime();

            return await _context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Conteneur)
                .Where(p => p.Actif && p.DatePaiement >= debutUtc && p.DatePaiement < finUtc)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetOverduePaymentsAsync()
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Where(p => p.Actif
                            && p.DateEcheance.HasValue
                            && p.DateEcheance.Value < DateTime.UtcNow
                            && p.Statut != StatutPaiement.Paye)
                .OrderBy(p => p.DateEcheance)
                .ToListAsync();
        }

        public async Task<decimal> GetMonthlyRevenueAsync(DateTime month)
        {
            var debutMois = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var debutMoisSuivant = debutMois.AddMonths(1);

            return await _context.Paiements
                .Where(p => p.Actif && p.Statut == StatutPaiement.Paye &&
                            p.DatePaiement >= debutMois &&
                            p.DatePaiement < debutMoisSuivant)
                .SumAsync(p => p.Montant);
        }
    }
}
