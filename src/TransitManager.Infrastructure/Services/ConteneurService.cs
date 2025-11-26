using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using TransitManager.Core.Exceptions;

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

        public async Task<Conteneur?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Client)
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Barcodes.Where(b => b.Actif))
                .Include(c => c.Vehicules)
                    .ThenInclude(v => v.Client)
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

		public async Task<Conteneur> CreateAsync(Conteneur conteneur)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			
			// === DÉBUT DE LA CORRECTION ===
			// On s'assure qu'on crée le conteneur "vide".
			// Les colis/véhicules seront ajoutés après sa création.
			conteneur.Colis = new List<Colis>();
			conteneur.Vehicules = new List<Vehicule>();
			// ==============================

			conteneur.Statut = CalculateStatusFromDates(conteneur);
			context.Conteneurs.Add(conteneur);
			await context.SaveChangesAsync();
			
			await _notificationService.NotifyAsync("Nouveau conteneur", $"Le conteneur {conteneur.NumeroDossier} pour {conteneur.Destination} a été créé.");
			return conteneur;
		}

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

			// === DÉBUT DE LA CORRECTION ===
			// On vide les listes de navigation de l'objet entrant pour éviter qu'EF Core 
			// n'essaie de suivre les Colis/Véhicules/Clients et ne provoque l'erreur de "Tracking".
			// La gestion des enfants (Ajout/Retrait) se fait via les méthodes Assign/Remove, pas ici.
			conteneurFromUI.Colis = new List<Colis>();
			conteneurFromUI.Vehicules = new List<Vehicule>();
			// ==============================

			var oldStatus = conteneurInDb.Statut;
			
			// Copie des valeurs scalaires (Numéro, Dates, etc.)
			context.Entry(conteneurInDb).CurrentValues.SetValues(conteneurFromUI);
			context.Entry(conteneurInDb).Property("RowVersion").OriginalValue = conteneurFromUI.RowVersion;
			
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
            if (problemColis || problemVehicule)
            {
                conteneur.Statut = StatutConteneur.Probleme;
            }
            else if ((conteneur.Colis.Any() || conteneur.Vehicules.Any()) && conteneur.Colis.All(c => c.Statut == StatutColis.Livre) && conteneur.Vehicules.All(v => v.Statut == StatutVehicule.Livre))
            {
                conteneur.Statut = StatutConteneur.Cloture;
                if (!conteneur.DateCloture.HasValue)
                {
                    conteneur.DateCloture = DateTime.UtcNow;
                }
            }
            else
            {
                conteneur.Statut = CalculateStatusFromDates(conteneur);
            }
            var currentStatusInDb = oldStatus ?? (await context.Entry(conteneur).GetDatabaseValuesAsync())?.GetValue<StatutConteneur>(nameof(Conteneur.Statut)) ?? conteneur.Statut;
            if (conteneur.Statut != currentStatusInDb)
            {
                await UpdateChildrenStatus(conteneur, context);
            }
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync();
                if (databaseValues == null)
                {
                    throw new ConcurrencyException("Ce dossier conteneur a été supprimé par un autre utilisateur.");
                }
                else
                {
                    throw new ConcurrencyException("Ce dossier conteneur a été modifié par un autre utilisateur. Vos modifications n'ont pas pu être enregistrées.");
                }
            }
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
            var ignoredColisStatuses = new[] { StatutColis.Probleme, StatutColis.Perdu, StatutColis.Retourne, StatutColis.Livre };
            var ignoredVehiculeStatuses = new[] { StatutVehicule.Probleme, StatutVehicule.Retourne, StatutVehicule.Livre };
            if (conteneur.Statut == StatutConteneur.Probleme)
            {
                return;
            }
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
                case StatutConteneur.Livre:
                    newColisStatus = StatutColis.Livre; newVehiculeStatus = StatutVehicule.Livre; break;
                default:
                    newColisStatus = StatutColis.Affecte; newVehiculeStatus = StatutVehicule.Affecte; break;
            }
            await context.Colis
                .Where(c => c.ConteneurId == conteneur.Id && !ignoredColisStatuses.Contains(c.Statut))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Statut, newColisStatus));
            await context.Vehicules
                .Where(v => v.ConteneurId == conteneur.Id && !ignoredVehiculeStatuses.Contains(v.Statut))
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Statut, newVehiculeStatus));
        }

        private async Task UpdatePlombOnChildren(Guid conteneurId, string? numeroPlomb, TransitContext context)
        {
            await context.Colis.Where(c => c.ConteneurId == conteneurId).ExecuteUpdateAsync(s => s.SetProperty(c => c.NumeroPlomb, numeroPlomb));
            await context.Vehicules.Where(v => v.ConteneurId == conteneurId).ExecuteUpdateAsync(s => s.SetProperty(v => v.NumeroPlomb, numeroPlomb));
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var conteneur = await context.Conteneurs
                .Include(c => c.Colis)
                .Include(c => c.Vehicules)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (conteneur == null) return false;
            if (conteneur.Colis.Any() || conteneur.Vehicules.Any())
            {
                throw new InvalidOperationException("Impossible de supprimer un conteneur qui n'est pas vide. Veuillez d'abord retirer tous les colis et véhicules.");
            }
            conteneur.Actif = false;
            conteneur.Statut = StatutConteneur.Annule;
            await context.SaveChangesAsync();
            return true;
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

        public Task<bool> CloseConteneurAsync(Guid id) => throw new NotImplementedException();

        public Task<bool> SetDepartureAsync(Guid id, DateTime departureDate) => throw new NotImplementedException();

        public Task<bool> SetArrivalAsync(Guid id, DateTime arrivalDate) => throw new NotImplementedException();

		public async Task<int> GetActiveCountAsync()
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var inactiveStatuses = new[] { StatutConteneur.Cloture, StatutConteneur.Annule, StatutConteneur.Livre };
			return await context.Conteneurs.CountAsync(c => c.Actif && !inactiveStatuses.Contains(c.Statut));
		}

		// Implémentons les autres méthodes avec des valeurs par défaut pour éviter de futures erreurs
		public Task<decimal> GetAverageFillingRateAsync()
		{
			// Logique complexe, on retourne 0 pour l'instant
			return Task.FromResult(0m);
    }

		public async Task<IEnumerable<Conteneur>> GetUpcomingDeparturesAsync(int days)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var upcomingDate = DateTime.UtcNow.AddDays(days);
			return await context.Conteneurs
				.Where(c => c.Actif && c.DateDepart.HasValue && c.DateDepart.Value >= DateTime.UtcNow && c.DateDepart.Value <= upcomingDate)
				.OrderBy(c => c.DateDepart)
				.ToListAsync();
		}

		// Pour cette méthode, nous allons faire une approximation. Une vraie mesure nécessiterait de connaître la capacité de chaque conteneur.
		// Nous allons considérer qu'un conteneur est "presque plein" s'il a plus de X colis.
		public async Task<IEnumerable<Conteneur>> GetAlmostFullContainersAsync(decimal threshold) // threshold sera un nombre de colis pour notre approximation
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var activeStatuses = new[] { StatutConteneur.Reçu, StatutConteneur.EnPreparation };
			return await context.Conteneurs
				.Where(c => c.Actif && activeStatuses.Contains(c.Statut) && (c.Colis.Count() + c.Vehicules.Count()) >= (int)threshold)
				.Include(c => c.Colis)
				.Include(c => c.Vehicules)
				.ToListAsync();
		}

		public Task<Dictionary<string, int>> GetStatsByDestinationAsync()
		{
			// Logique à implémenter plus tard
			return Task.FromResult(new Dictionary<string, int>());
		}


        public Task<bool> CanAddColisAsync(Guid conteneurId, Guid colisId) => throw new NotImplementedException();

        public Task<decimal> CalculateProfitabilityAsync(Guid conteneurId) => throw new NotImplementedException();
		
		public async Task<IEnumerable<Conteneur>> GetByClientIdAsync(Guid clientId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			
			// On récupère les conteneurs où :
			// - Soit il y a un colis appartenant au client
			// - Soit il y a un véhicule appartenant au client
			return await context.Conteneurs
				.Include(c => c.Colis)
				.Include(c => c.Vehicules)
				.Where(c => c.Actif && (
					c.Colis.Any(col => col.ClientId == clientId && col.Actif) || 
					c.Vehicules.Any(v => v.ClientId == clientId && v.Actif)
				))
				.OrderByDescending(c => c.DateCreation)
				.AsNoTracking()
				.ToListAsync();
		}
		
    }
}
