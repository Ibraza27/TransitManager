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

        public async Task<Conteneur> UpdateAsync(Conteneur conteneurFromUI)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            var conteneurInDb = await context.Conteneurs
                .Include(c => c.Colis)
                .Include(c => c.Vehicules)
                .FirstOrDefaultAsync(c => c.Id == conteneurFromUI.Id);

            if (conteneurInDb == null) throw new Exception("Conteneur non trouvé.");
            
            var oldStatus = conteneurInDb.Statut;
            
            // On applique les changements de l'UI (y compris la DateCloture = null)
            context.Entry(conteneurInDb).CurrentValues.SetValues(conteneurFromUI);

            // On recalcule le statut basé sur les nouvelles valeurs
            var newStatus = CalculateStatus(conteneurInDb);
            conteneurInDb.Statut = newStatus;

            // On compare l'ancien statut de la BDD avec le nouveau statut calculé
            if (newStatus != oldStatus)
            {
                await UpdateChildrenStatus(conteneurInDb, newStatus, context);
            }
            
            if (conteneurInDb.Colis.All(c => c.Statut == StatutColis.Livre) &&
                conteneurInDb.Vehicules.All(v => v.Statut == StatutVehicule.Livre) &&
                conteneurInDb.Statut != StatutConteneur.Cloture)
            {
                conteneurInDb.Statut = StatutConteneur.Cloture;
                if (!conteneurInDb.DateCloture.HasValue) conteneurInDb.DateCloture = DateTime.UtcNow;
            }

            if (conteneurInDb.NumeroPlomb != conteneurFromUI.NumeroPlomb)
            {
                await UpdatePlombOnChildren(conteneurInDb.Id, conteneurInDb.NumeroPlomb, context);
            }

            await context.SaveChangesAsync();
            return conteneurInDb;
        }

        private StatutConteneur CalculateStatus(Conteneur conteneur)
        {
            if (conteneur.DateCloture.HasValue) return StatutConteneur.Cloture;
            if (conteneur.DateDedouanement.HasValue) return StatutConteneur.EnDedouanement;
            if (conteneur.DateArriveeDestination.HasValue) return StatutConteneur.Arrive;
            if (conteneur.DateDepart.HasValue) return StatutConteneur.EnTransit;
            if (conteneur.DateChargement.HasValue) return StatutConteneur.EnPreparation;
            if (conteneur.DateReception.HasValue) return StatutConteneur.Reçu;
            return StatutConteneur.Reçu;
        }

        private async Task UpdateChildrenStatus(Conteneur conteneur, StatutConteneur newContainerStatus, TransitContext context)
        {
            StatutColis newColisStatus;
            StatutVehicule newVehiculeStatus;

            switch (newContainerStatus)
            {
                case StatutConteneur.EnTransit:
                    newColisStatus = StatutColis.EnTransit; newVehiculeStatus = StatutVehicule.EnTransit; break;
                case StatutConteneur.Arrive:
                    newColisStatus = StatutColis.Arrive; newVehiculeStatus = StatutVehicule.Arrive; break;
                case StatutConteneur.EnDedouanement:
                    newColisStatus = StatutColis.EnDedouanement; newVehiculeStatus = StatutVehicule.EnDedouanement; break;
                case StatutConteneur.Cloture:
                    newColisStatus = StatutColis.Livre; newVehiculeStatus = StatutVehicule.Livre; break;
                default: // Reçu, EnPreparation, ou retour à un état précédent
                    newColisStatus = StatutColis.Affecte; newVehiculeStatus = StatutVehicule.Affecte; break;
            }
            await SetChildrenStatus(conteneur.Id, newColisStatus, newVehiculeStatus, context);
        }

        private async Task SetChildrenStatus(Guid conteneurId, StatutColis newColisStatus, StatutVehicule newVehiculeStatus, TransitContext context)
        {
            var problemStatus = new[] { StatutColis.Probleme, StatutColis.Perdu, StatutColis.Retourne };
            await context.Colis
                .Where(c => c.ConteneurId == conteneurId && !problemStatus.Contains(c.Statut))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Statut, newColisStatus));

            var problemVehiculeStatus = new[] { StatutVehicule.Probleme, StatutVehicule.Retourne };
            await context.Vehicules
                .Where(v => v.ConteneurId == conteneurId && !problemVehiculeStatus.Contains(v.Statut))
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Statut, newVehiculeStatus));
        }

        // --- Le reste du service ne change pas ---
        #region Reste du service (inchangé)
        public async Task<Conteneur> CreateAsync(Conteneur conteneur)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            conteneur.Statut = CalculateStatus(conteneur);
            context.Conteneurs.Add(conteneur);
            await context.SaveChangesAsync();
            await _notificationService.NotifyAsync("Nouveau conteneur", $"Le conteneur {conteneur.NumeroDossier} pour {conteneur.Destination} a été créé.");
            return conteneur;
        }
        private async Task UpdatePlombOnChildren(Guid conteneurId, string? numeroPlomb, TransitContext context)
        {
            await context.Colis.Where(c => c.ConteneurId == conteneurId).ExecuteUpdateAsync(s => s.SetProperty(c => c.NumeroPlomb, numeroPlomb));
            await context.Vehicules.Where(v => v.ConteneurId == conteneurId).ExecuteUpdateAsync(s => s.SetProperty(v => v.NumeroPlomb, numeroPlomb));
        }
        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var conteneur = await context.Conteneurs.Include(c => c.Colis).Include(c => c.Vehicules).FirstOrDefaultAsync(c => c.Id == id);
            if (conteneur == null) return false;
            if (conteneur.Colis.Any() || conteneur.Vehicules.Any()) throw new InvalidOperationException("Impossible de supprimer un conteneur contenant des colis ou des véhicules.");
            conteneur.Actif = false;
            conteneur.Statut = StatutConteneur.Annule;
            await context.SaveChangesAsync();
            return true;
        }
        public async Task<Conteneur?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs.Include(c => c.Colis).ThenInclude(col => col.Client).Include(c => c.Vehicules).ThenInclude(v => v.Client).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }
        public async Task<IEnumerable<Conteneur>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs.Include(c => c.Colis).Include(c => c.Vehicules).AsNoTracking().OrderByDescending(c => c.DateCreation).ToListAsync();
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