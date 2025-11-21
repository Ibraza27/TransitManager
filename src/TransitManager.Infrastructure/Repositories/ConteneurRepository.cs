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
    public interface IConteneurRepository : IGenericRepository<Conteneur>
    {
        Task<Conteneur?> GetByNumeroDossierAsync(string numeroDossier);
        Task<Conteneur?> GetWithDetailsAsync(Guid id);
        Task<IEnumerable<Conteneur>> GetOpenConteneursAsync();
        Task<IEnumerable<Conteneur>> GetByDestinationAsync(string destination);
        Task<IEnumerable<Conteneur>> GetByStatusAsync(StatutConteneur statut);
        Task<IEnumerable<string>> GetAllDestinationsAsync();
        Task<IEnumerable<Conteneur>> SearchAsync(string searchTerm);
    }

    public class ConteneurRepository : GenericRepository<Conteneur>, IConteneurRepository
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;

        public ConteneurRepository(IDbContextFactory<TransitContext> contextFactory) : base(contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Conteneur?> GetByNumeroDossierAsync(string numeroDossier)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Conteneur>()
                .Include(c => c.Colis).ThenInclude(col => col.Client)
                .Include(c => c.Vehicules).ThenInclude(v => v.Client)
                .FirstOrDefaultAsync(c => c.NumeroDossier == numeroDossier && c.Actif);
        }

        public async Task<Conteneur?> GetWithDetailsAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Conteneur>()
                .Include(c => c.Colis).ThenInclude(col => col.Client)
                .Include(c => c.Vehicules).ThenInclude(v => v.Client)
                .FirstOrDefaultAsync(c => c.Id == id && c.Actif);
        }

        public async Task<IEnumerable<Conteneur>> GetOpenConteneursAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var openStatuses = new[] { StatutConteneur.Reçu, StatutConteneur.EnPreparation };
            return await context.Set<Conteneur>()
                .Include(c => c.Colis)
                .Include(c => c.Vehicules)
                .Where(c => c.Actif && openStatuses.Contains(c.Statut))
                .OrderByDescending(c => c.DateReception)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetByDestinationAsync(string destination)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(destination))
                return await GetAllAsync();
            return await context.Set<Conteneur>()
                .Include(c => c.Colis)
                .Where(c => c.Actif && (
                    c.Destination.ToLower().Contains(destination.ToLower()) ||
                    c.PaysDestination.ToLower().Contains(destination.ToLower())
                ))
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetByStatusAsync(StatutConteneur statut)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Conteneur>()
                .Include(c => c.Colis)
                .Where(c => c.Actif && c.Statut == statut)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetAllDestinationsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var destinations = await context.Set<Conteneur>()
                .Where(c => c.Actif)
                .Select(c => c.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
            return destinations;
        }

        public async Task<IEnumerable<Conteneur>> SearchAsync(string searchTerm)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();
            searchTerm = searchTerm.ToLower();
            return await context.Set<Conteneur>()
                .Include(c => c.Colis).ThenInclude(col => col.Client)
                .Include(c => c.Vehicules).ThenInclude(v => v.Client)
                .Where(c => c.Actif && (
                    c.NumeroDossier.ToLower().Contains(searchTerm) ||
                    c.Destination.ToLower().Contains(searchTerm) ||
                    c.PaysDestination.ToLower().Contains(searchTerm) ||
                    (c.NomCompagnie != null && c.NomCompagnie.ToLower().Contains(searchTerm)) ||
                    (c.NumeroPlomb != null && c.NumeroPlomb.ToLower().Contains(searchTerm)) ||
                    (c.NomTransitaire != null && c.NomTransitaire.ToLower().Contains(searchTerm))
                ))
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }
    }
}
