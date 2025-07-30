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
        Task<IEnumerable<Conteneur>> GetUpcomingDeparturesAsync(int days);
        Task<IEnumerable<Conteneur>> GetRecentArrivalsAsync(int days);
        Task<IEnumerable<string>> GetAllDestinationsAsync();
        Task<Dictionary<string, decimal>> GetFillingRatesByDestinationAsync();
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
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Client)
                .FirstOrDefaultAsync(c => c.NumeroDossier == numeroDossier && c.Actif);
        }

        public async Task<Conteneur?> GetWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Client)
                .FirstOrDefaultAsync(c => c.Id == id && c.Actif);
        }

        public async Task<IEnumerable<Conteneur>> GetOpenConteneursAsync()
        {
            return await _dbSet
                .Include(c => c.Colis)
                .Where(c => c.Actif && 
                       (c.Statut == StatutConteneur.Ouvert || 
                        c.Statut == StatutConteneur.EnPreparation))
                .OrderByDescending(c => c.DateOuverture)
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
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetByStatusAsync(StatutConteneur statut)
        {
            return await _dbSet
                .Include(c => c.Colis)
                .Where(c => c.Actif && c.Statut == statut)
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetUpcomingDeparturesAsync(int days)
        {
            var dateLimit = DateTime.UtcNow.AddDays(days);

            return await _dbSet
                .Include(c => c.Colis)
                .Where(c => c.Actif && 
                       c.DateDepartPrevue != null &&
                       c.DateDepartPrevue.Value >= DateTime.UtcNow &&
                       c.DateDepartPrevue.Value <= dateLimit &&
                       (c.Statut == StatutConteneur.Ouvert || 
                        c.Statut == StatutConteneur.EnPreparation))
                .OrderBy(c => c.DateDepartPrevue)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetRecentArrivalsAsync(int days)
        {
            var dateLimit = DateTime.UtcNow.AddDays(-days);

            return await _dbSet
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Client)
                .Where(c => c.Actif && 
                       c.DateArriveeReelle != null &&
                       c.DateArriveeReelle.Value >= dateLimit &&
                       c.Statut == StatutConteneur.Arrive)
                .OrderByDescending(c => c.DateArriveeReelle)
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

            var pays = await _dbSet
                .Where(c => c.Actif)
                .Select(c => c.PaysDestination)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();

            return destinations.Union(pays).Distinct().OrderBy(d => d);
        }

        public async Task<Dictionary<string, decimal>> GetFillingRatesByDestinationAsync()
        {
            var conteneurs = await _dbSet
                .Where(c => c.Actif && 
                       (c.Statut == StatutConteneur.Ouvert || 
                        c.Statut == StatutConteneur.EnPreparation))
                .ToListAsync();

            return conteneurs
                .GroupBy(c => c.PaysDestination)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(c => (c.TauxRemplissageVolume + c.TauxRemplissagePoids) / 2)
                );
        }

        public async Task<IEnumerable<Conteneur>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            searchTerm = searchTerm.ToLower();

            return await _dbSet
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Client)
                .Where(c => c.Actif && (
                    c.NumeroDossier.ToLower().Contains(searchTerm) ||
                    c.Destination.ToLower().Contains(searchTerm) ||
                    c.PaysDestination.ToLower().Contains(searchTerm) ||
                    (c.Transporteur != null && c.Transporteur.ToLower().Contains(searchTerm)) ||
                    (c.NumeroTracking != null && c.NumeroTracking.ToLower().Contains(searchTerm)) ||
                    (c.NumeroNavireVol != null && c.NumeroNavireVol.ToLower().Contains(searchTerm))
                ))
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }
    }
}