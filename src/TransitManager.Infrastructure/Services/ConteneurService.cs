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

        // NOUVELLE MÉTHODE PUBLIQUE POUR RECALCULER LE STATUT
        public async Task RecalculateStatusAsync(Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var conteneur = await context.Conteneurs
                .Include(c => c.Colis)
                .Include(c => c.Vehicules)
                .FirstOrDefaultAsync(c => c.Id == conteneurId);

            if (conteneur == null) return;

            await UpdateAndSaveConteneurStatus(conteneur, context);
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
            
            context.Entry(conteneurInDb).CurrentValues.SetValues(conteneurFromUI);

            // La logique de mise à jour du statut est maintenant dans une méthode séparée
            await UpdateAndSaveConteneurStatus(conteneurInDb, context, oldStatus);

            if (conteneurInDb.NumeroPlomb != conteneurFromUI.NumeroPlomb)
            {
                await UpdatePlombOnChildren(conteneurInDb.Id, conteneurInDb.NumeroPlomb, context);
            }

            return conteneurInDb;
        }

        private async Task UpdateAndSaveConteneurStatus(Conteneur conteneur, TransitContext context, StatutConteneur? oldStatus = null)
        {
            var problemColis = conteneur.Colis.Any(c => c.Statut == StatutColis.Probleme || c.Statut == StatutColis.Perdu);
            var problemVehicule = conteneur.Vehicules.Any(v => v.Statut == StatutVehicule.Probleme);

            // Règle 1 : Un élément en problème met le conteneur en problème
            if (problemColis || problemVehicule)
            {
                conteneur.Statut = StatutConteneur.Probleme;
            }
            // Règle 2 : Si tous les éléments sont livrés ET que le conteneur n'est pas vide, il est clôturé
            // <-- MODIFICATION CLÉ : Ajout de la vérification que le conteneur n'est pas vide
            else if ((conteneur.Colis.Any() || conteneur.Vehicules.Any()) && conteneur.Colis.All(c => c.Statut == StatutColis.Livre) && conteneur.Vehicules.All(v => v.Statut == StatutVehicule.Livre))
            {
                conteneur.Statut = StatutConteneur.Cloture;
                if (!conteneur.DateCloture.HasValue) 
                {
                    conteneur.DateCloture = DateTime.UtcNow;
                }
            }
            // Règle 3 : Sinon (y compris s'il est vide), le statut est basé sur les dates
            else
            {
                conteneur.Statut = CalculateStatusFromDates(conteneur);
            }
            
            var currentStatusInDb = oldStatus ?? (await context.Entry(conteneur).GetDatabaseValuesAsync())?.GetValue<StatutConteneur>(nameof(Conteneur.Statut)) ?? conteneur.Statut;

            if (conteneur.Statut != currentStatusInDb)
            {
                await UpdateChildrenStatus(conteneur, context);
            }
            
            await context.SaveChangesAsync();
        }

        private StatutConteneur CalculateStatusFromDates(Conteneur conteneur)
        {
            if (conteneur.DateCloture.HasValue) return StatutConteneur.Cloture;
            if (conteneur.DateDedouanement.HasValue) return StatutConteneur.EnDedouanement;
            if (conteneur.DateArriveeDestination.HasValue) return StatutConteneur.Arrive;
            if (conteneur.DateDepart.HasValue) return StatutConteneur.EnTransit;
            if (conteneur.DateChargement.HasValue) return StatutConteneur.EnPreparation;
            if (conteneur.DateReception.HasValue) return StatutConteneur.Reçu;
            return StatutConteneur.Reçu;
        }

        private async Task UpdateChildrenStatus(Conteneur conteneur, TransitContext context)
        {
            // Statuts "manuels" qui priment et ne doivent JAMAIS être écrasés par une synchro de conteneur.
            var ignoredColisStatuses = new[] { StatutColis.Probleme, StatutColis.Perdu, StatutColis.Retourne, StatutColis.Livre };
            var ignoredVehiculeStatuses = new[] { StatutVehicule.Probleme, StatutVehicule.Retourne, StatutVehicule.Livre };

            // On ne fait rien si le conteneur passe en problème, car on ne veut pas écraser les statuts des enfants "sains".
            if (conteneur.Statut == StatutConteneur.Probleme)
            {
                return;
            }

            // Déterminer le statut cible pour les enfants en fonction du nouveau statut du conteneur.
            StatutColis newColisStatus;
            StatutVehicule newVehiculeStatus;
            
            switch (conteneur.Statut)
            {
                case StatutConteneur.EnTransit:
                    newColisStatus = StatutColis.EnTransit; newVehiculeStatus = StatutVehicule.EnTransit; break;
                case StatutConteneur.Arrive:
                    newColisStatus = StatutColis.Arrive; newVehiculeStatus = StatutVehicule.Arrive; break;
                case StatutConteneur.EnDedouanement:
                    newColisStatus = StatutColis.EnDedouanement; newVehiculeStatus = StatutVehicule.EnDedouanement; break;
                case StatutConteneur.Cloture:
                case StatutConteneur.Livre: // Au cas où
                    newColisStatus = StatutColis.Livre; newVehiculeStatus = StatutVehicule.Livre; break;
                default: // Pour Reçu, EnPreparation, etc., le statut synchronisé est Affecte.
                    newColisStatus = StatutColis.Affecte; newVehiculeStatus = StatutVehicule.Affecte; break;
            }

            // Appliquer la mise à jour uniquement aux éléments qui n'ont pas un statut manuel prioritaire.
            await context.Colis
                .Where(c => c.ConteneurId == conteneur.Id && !ignoredColisStatuses.Contains(c.Statut))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Statut, newColisStatus));

            await context.Vehicules
                .Where(v => v.ConteneurId == conteneur.Id && !ignoredVehiculeStatuses.Contains(v.Statut))
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Statut, newVehiculeStatus));
        }

        #region Reste du service (inchangé)
        public async Task<Conteneur> CreateAsync(Conteneur conteneur)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            conteneur.Statut = CalculateStatusFromDates(conteneur);
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
			return await context.Conteneurs
				.IgnoreQueryFilters()
				.Include(c => c.Colis)
					.ThenInclude(col => col.Client)
				.Include(c => c.Colis)
					.ThenInclude(col => col.Barcodes.Where(b => b.Actif))
				.Include(c => c.Vehicules)
					.ThenInclude(v => v.Client)
				// .AsNoTracking() // <--- ON A RETIRÉ CETTE LIGNE
				.FirstOrDefaultAsync(c => c.Id == id);
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