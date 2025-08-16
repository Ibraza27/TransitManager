using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class ConteneurService : IConteneurService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly INotificationService _notificationService;

        public ConteneurService(IDbContextFactory<TransitContext> contextFactory, INotificationService notificationService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
        }

        // --- Méthode privée pour calculer le statut ---
        private StatutConteneur CalculateStatus(Conteneur conteneur)
        {
            // Note : L'ordre est important, du plus avancé au moins avancé.
            if (conteneur.DateDedouanement.HasValue) return StatutConteneur.EnDedouanement;
            if (conteneur.DateArriveeDestination.HasValue) return StatutConteneur.Arrive;
            if (conteneur.DateDepart.HasValue) return StatutConteneur.EnTransit;
            if (conteneur.DateChargement.HasValue) return StatutConteneur.EnPreparation;
            
            // Le statut de base si au moins une date de réception est présente
            if (conteneur.DateReception.HasValue) return StatutConteneur.Reçu;

            // Statut par défaut (ne devrait pas être atteint si la validation de l'UI est correcte)
            return StatutConteneur.Reçu;
        }

        public async Task<Conteneur> UpdateAsync(Conteneur conteneur)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            var original = await context.Conteneurs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == conteneur.Id);
            if (original == null) throw new Exception("Conteneur non trouvé.");

            // On recalcule TOUJOURS le statut avant de sauvegarder.
            conteneur.Statut = CalculateStatus(conteneur);

            // Mettre à jour le numéro de plomb sur les colis et véhicules s'il a changé
            if (conteneur.NumeroPlomb != original.NumeroPlomb)
            {
                await UpdatePlombOnChildren(conteneur.Id, conteneur.NumeroPlomb, context);
            }

            context.Conteneurs.Update(conteneur);
            await context.SaveChangesAsync();
            return conteneur;
        }

        public async Task<Conteneur> CreateAsync(Conteneur conteneur)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // On s'assure que le statut est correct dès la création
            conteneur.Statut = CalculateStatus(conteneur);

            context.Conteneurs.Add(conteneur);
            await context.SaveChangesAsync();

            await _notificationService.NotifyAsync("Nouveau conteneur", $"Le conteneur {conteneur.NumeroDossier} pour {conteneur.Destination} a été créé.");
            return conteneur;
        }

        private async Task UpdatePlombOnChildren(Guid conteneurId, string? numeroPlomb, TransitContext context)
        {
            // On utilise ExecuteUpdateAsync pour une mise à jour en masse, c'est plus performant.
            await context.Colis
                .Where(c => c.ConteneurId == conteneurId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.NumeroPlomb, numeroPlomb));

            await context.Vehicules
                .Where(v => v.ConteneurId == conteneurId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.NumeroPlomb, numeroPlomb));
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var conteneur = await context.Conteneurs.Include(c => c.Colis).Include(c => c.Vehicules).FirstOrDefaultAsync(c => c.Id == id);
            if (conteneur == null) return false;

            if (conteneur.Colis.Any() || conteneur.Vehicules.Any())
            {
                throw new InvalidOperationException("Impossible de supprimer un conteneur contenant des colis ou des véhicules.");
            }

            conteneur.Actif = false;
            conteneur.Statut = StatutConteneur.Annule;
            await context.SaveChangesAsync();
            return true;
        }

        #region Méthodes de lecture et autres (implémentations de l'interface)

        public async Task<Conteneur?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis).ThenInclude(col => col.Client)
                .Include(c => c.Vehicules).ThenInclude(v => v.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Conteneur>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis)
                .Include(c => c.Vehicules)
                .AsNoTracking()
                .OrderByDescending(c => c.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetActiveAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var inactiveStatuses = new[] { StatutConteneur.Cloture, StatutConteneur.Annule };
            return await context.Conteneurs.Where(c => c.Actif && !inactiveStatuses.Contains(c.Statut)).ToListAsync();
        }
        
        public async Task<IEnumerable<string>> GetAllDestinationsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs.Where(c => c.Actif).Select(c => c.Destination).Distinct().OrderBy(d => d).ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetOpenConteneursAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var openStatuses = new[] { StatutConteneur.Reçu, StatutConteneur.EnPreparation };
            return await context.Conteneurs.Where(c => c.Actif && openStatuses.Contains(c.Statut)).ToListAsync();
        }
        
        public async Task<Conteneur?> GetByNumeroDossierAsync(string numeroDossier)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs.FirstOrDefaultAsync(c => c.NumeroDossier == numeroDossier);
        }

        public async Task<IEnumerable<Conteneur>> GetByDestinationAsync(string destination)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs.Where(c => c.Destination == destination).ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetByStatusAsync(StatutConteneur statut)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs.Where(c => c.Statut == statut).ToListAsync();
        }

        // Les méthodes suivantes sont des placeholders pour les fonctionnalités futures.
        public Task<bool> CloseConteneurAsync(Guid id) => Task.FromResult(true);
        public Task<bool> SetDepartureAsync(Guid id, DateTime departureDate) => Task.FromResult(true);
        public Task<bool> SetArrivalAsync(Guid id, DateTime arrivalDate) => Task.FromResult(true);
        public async Task<int> GetActiveCountAsync() => (await GetActiveAsync()).Count();
        public Task<decimal> GetAverageFillingRateAsync() => Task.FromResult(0m);
        public Task<IEnumerable<Conteneur>> GetUpcomingDeparturesAsync(int days) => Task.FromResult(Enumerable.Empty<Conteneur>());
        public Task<IEnumerable<Conteneur>> GetAlmostFullContainersAsync(decimal threshold) => Task.FromResult(Enumerable.Empty<Conteneur>());
        public Task<Dictionary<string, int>> GetStatsByDestinationAsync() => Task.FromResult(new Dictionary<string, int>());
        public Task<bool> CanAddColisAsync(Guid conteneurId, Guid colisId) => Task.FromResult(true);
        public Task<decimal> CalculateProfitabilityAsync(Guid conteneurId) => Task.FromResult(0m);
        #endregion
    }
}