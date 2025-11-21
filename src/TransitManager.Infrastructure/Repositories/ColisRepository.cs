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
    public interface IColisRepository : IGenericRepository<Colis>
    {
        Task<Colis?> GetByBarcodeAsync(string barcode);
        Task<Colis?> GetByReferenceAsync(string reference);
        Task<Colis?> GetWithDetailsAsync(Guid id);
        Task<IEnumerable<Colis>> GetByClientAsync(Guid clientId);
        Task<IEnumerable<Colis>> GetByConteneurAsync(Guid conteneurId);
        Task<IEnumerable<Colis>> GetByStatusAsync(StatutColis statut);
        Task<IEnumerable<Colis>> GetUnassignedAsync();
        Task<IEnumerable<Colis>> GetRecentAsync(int count);
        Task<IEnumerable<Colis>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<StatutColis, int>> GetStatisticsByStatusAsync();
        Task<IEnumerable<Colis>> SearchAsync(string searchTerm);
    }

    public class ColisRepository : GenericRepository<Colis>, IColisRepository
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;

        public ColisRepository(IDbContextFactory<TransitContext> contextFactory) : base(contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Colis?> GetByBarcodeAsync(string barcode)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .FirstOrDefaultAsync(c => c.Barcodes.Any(b => b.Value == barcode) && c.Actif);
        }

        public async Task<Colis?> GetByReferenceAsync(string reference)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .FirstOrDefaultAsync(c => c.NumeroReference == reference && c.Actif);
        }

        public async Task<Colis?> GetWithDetailsAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .FirstOrDefaultAsync(c => c.Id == id && c.Actif);
        }

        public async Task<IEnumerable<Colis>> GetByClientAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Include(c => c.Conteneur)
                .Where(c => c.ClientId == clientId && c.Actif)
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByConteneurAsync(Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Include(c => c.Client)
                .Where(c => c.ConteneurId == conteneurId && c.Actif)
                .OrderBy(c => c.Client!.Nom)
                .ThenBy(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByStatusAsync(StatutColis statut)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Where(c => c.Statut == statut && c.Actif)
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetUnassignedAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Include(c => c.Client)
                .Where(c => c.ConteneurId == null && c.Actif && c.Statut == StatutColis.EnAttente)
                .OrderBy(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetRecentAsync(int count)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Include(c => c.Client)
                .Where(c => c.Actif)
                .OrderByDescending(c => c.DateArrivee)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Where(c => c.DateArrivee >= startDate && c.DateArrivee <= endDate && c.Actif)
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<Dictionary<StatutColis, int>> GetStatisticsByStatusAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Colis>()
                .Where(c => c.Actif)
                .GroupBy(c => c.Statut)
                .Select(g => new { Statut = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Statut, x => x.Count);
        }

        public async Task<IEnumerable<Colis>> SearchAsync(string searchTerm)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();
            searchTerm = searchTerm.ToLower();
            return await context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Where(c => c.Actif && (
                    c.Barcodes.Any(b => b.Value.Contains(searchTerm)) ||
                    c.NumeroReference.ToLower().Contains(searchTerm) ||
                    c.Designation.ToLower().Contains(searchTerm) ||
                    (c.Client != null && (
                        c.Client.Nom.ToLower().Contains(searchTerm) ||
                        c.Client.Prenom.ToLower().Contains(searchTerm) ||
                        (c.Client.Nom + " " + c.Client.Prenom).ToLower().Contains(searchTerm)
                    )) ||
                    (c.Conteneur != null && c.Conteneur.NumeroDossier.ToLower().Contains(searchTerm)) ||
                    (c.Destinataire != null && c.Destinataire.ToLower().Contains(searchTerm))
                ))
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }
    }
}
