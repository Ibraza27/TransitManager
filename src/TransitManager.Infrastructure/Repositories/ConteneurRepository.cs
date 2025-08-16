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
        public ConteneurRepository(TransitContext context) : base(context)
        {
        }

        public async Task<Conteneur?> GetByNumeroDossierAsync(string numeroDossier)
        {
            return await _dbSet
                .Include(c => c.Colis).ThenInclude(col => col.Client)
                .Include(c => c.Vehicules).ThenInclude(v => v.Client)
                .FirstOrDefaultAsync(c => c.NumeroDossier == numeroDossier && c.Actif);
        }

        public async Task<Conteneur?> GetWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.Colis).ThenInclude(col => col.Client)
                .Include(c => c.Vehicules).ThenInclude(v => v.Client)
                .FirstOrDefaultAsync(c => c.Id == id && c.Actif);
        }

        public async Task<IEnumerable<Conteneur>> GetOpenConteneursAsync()
        {
            var openStatuses = new[] { StatutConteneur.Reçu, StatutConteneur.EnPreparation };
            return await _dbSet
                .Include(c => c.Colis)
                .Include(c => c.Vehicules)
                .Where(c => c.Actif && openStatuses.Contains(c.Statut))
                .OrderByDescending(c => c.DateReception)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetByDestinationAsync(string destination)
        {
            if (string.IsNullOrWhiteSpace(destination))
                return await GetAllAsync();

            return await _dbSet
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
            return await _dbSet
                .Include(c => c.Colis)
                .Where(c => c.Actif && c.Statut == statut)
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetAllDestinationsAsync()
        {
            var destinations = await _dbSet
                .Where(c => c.Actif)
                .Select(c => c.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            return destinations;
        }

        public async Task<IEnumerable<Conteneur>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            searchTerm = searchTerm.ToLower();

            return await _dbSet
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