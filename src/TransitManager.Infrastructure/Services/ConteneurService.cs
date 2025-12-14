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
        private readonly ITimelineService _timelineService;
        
        // --- AJOUT : Service d'authentification ---
        private readonly IAuthenticationService _authService;

        public ConteneurService(
            IDbContextFactory<TransitContext> contextFactory, 
            INotificationService notificationService,
            ITimelineService timelineService,
            IAuthenticationService authService) // --- AJOUT ---
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
            _timelineService = timelineService;
            _authService = authService; // --- AJOUT ---
        }

        // ... GetByIdAsync, GetAllAsync, CreateAsync (Inchangés) ...
        
        public async Task<Conteneur?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(c => c.Colis).ThenInclude(col => col.Client)
                .Include(c => c.Colis).ThenInclude(col => col.Barcodes.Where(b => b.Actif))
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

        public async Task<Conteneur> CreateAsync(Conteneur conteneur)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            conteneur.Colis = new List<Colis>();
            conteneur.Vehicules = new List<Vehicule>();
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

            var oldStatus = conteneurInDb.Statut;

            context.Entry(conteneurInDb).CurrentValues.SetValues(conteneurFromUI);
            context.Entry(conteneurInDb).Property("RowVersion").OriginalValue = conteneurFromUI.RowVersion;

            await UpdateAndSaveConteneurStatus(conteneurInDb, context, oldStatus);

            if (conteneurInDb.NumeroPlomb != conteneurFromUI.NumeroPlomb)
            {
                await UpdatePlombOnChildren(conteneurInDb.Id, conteneurInDb.NumeroPlomb, context);
            }

            // Notification ADMIN pour le conteneur lui-même
            if (oldStatus != conteneurInDb.Statut)
            {
                await _notificationService.CreateAndSendAsync(
                    "🚢 Statut Conteneur",
                    $"Le conteneur {conteneurInDb.NumeroDossier} est passé à : {conteneurInDb.Statut}",
                    null, // Admin
                    CategorieNotification.StatutConteneur,
                    actionUrl: $"/conteneur/detail/{conteneurInDb.Id}"
                );
            }

            return conteneurInDb;
        }

        private async Task UpdateAndSaveConteneurStatus(Conteneur conteneur, TransitContext context, StatutConteneur? oldStatus = null)
        {
            // ... (Calcul statut inchangé) ...
            var problemColis = conteneur.Colis.Any(c => c.Statut == StatutColis.Probleme || c.Statut == StatutColis.Perdu);
            var problemVehicule = conteneur.Vehicules.Any(v => v.Statut == StatutVehicule.Probleme);
            if (problemColis || problemVehicule)
            {
                conteneur.Statut = StatutConteneur.Probleme;
            }
            else if ((conteneur.Colis.Any() || conteneur.Vehicules.Any()) && conteneur.Colis.All(c => c.Statut == StatutColis.Livre) && conteneur.Vehicules.All(v => v.Statut == StatutVehicule.Livre))
            {
                conteneur.Statut = StatutConteneur.Cloture;
                if (!conteneur.DateCloture.HasValue) conteneur.DateCloture = DateTime.UtcNow;
            }
            else
            {
                conteneur.Statut = CalculateStatusFromDates(conteneur);
            }

            var currentStatusInDb = oldStatus ?? (await context.Entry(conteneur).GetDatabaseValuesAsync())?.GetValue<StatutConteneur>(nameof(Conteneur.Statut)) ?? conteneur.Statut;
            
            // Si le statut a changé, on met à jour les enfants ET on notifie
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
               throw new ConcurrencyException("Conflit de concurrence.");
            }
        }

        // ... CalculateStatusFromDates (Inchangé) ...
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
            
            if (conteneur.Statut == StatutConteneur.Probleme) return;

            StatutColis newColisStatus;
            StatutVehicule newVehiculeStatus;
            
            // Mapping du statut
            switch (conteneur.Statut)
            {
                case StatutConteneur.EnTransit: newColisStatus = StatutColis.EnTransit; newVehiculeStatus = StatutVehicule.EnTransit; break;
                case StatutConteneur.Arrive: newColisStatus = StatutColis.Arrive; newVehiculeStatus = StatutVehicule.Arrive; break;
                case StatutConteneur.EnDedouanement: newColisStatus = StatutColis.EnDedouanement; newVehiculeStatus = StatutVehicule.EnDedouanement; break;
                case StatutConteneur.Cloture:
                case StatutConteneur.Livre: newColisStatus = StatutColis.Livre; newVehiculeStatus = StatutVehicule.Livre; break;
                default: newColisStatus = StatutColis.Affecte; newVehiculeStatus = StatutVehicule.Affecte; break;
            }

            // === DÉBUT DE LA CORRECTION : RÉCUPÉRATION POUR NOTIFICATION ===
            
            // 1. Récupérer les Colis concernés AVEC le Client
            var colisToUpdate = await context.Colis
                .Where(c => c.ConteneurId == conteneur.Id && !ignoredColisStatuses.Contains(c.Statut))
                .Include(c => c.Client).ThenInclude(cl => cl.UserAccount) // Important pour avoir l'ID User
                .ToListAsync();

            // 2. Récupérer les Véhicules concernés AVEC le Client
            var vehiculesToUpdate = await context.Vehicules
                .Where(v => v.ConteneurId == conteneur.Id && !ignoredVehiculeStatuses.Contains(v.Statut))
                .Include(v => v.Client).ThenInclude(cl => cl.UserAccount)
                .ToListAsync();

            // 3. Mise à jour de masse (Performante)
            await context.Colis
                .Where(c => c.ConteneurId == conteneur.Id && !ignoredColisStatuses.Contains(c.Statut))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Statut, newColisStatus));
            
            await context.Vehicules
                .Where(v => v.ConteneurId == conteneur.Id && !ignoredVehiculeStatuses.Contains(v.Statut))
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.Statut, newVehiculeStatus));

            // 4. BOUCLE DE NOTIFICATION CLIENTS
            string descriptionEvent = $"Mise à jour via conteneur {conteneur.NumeroDossier} : {conteneur.Statut}";

            // Pour les Colis
            foreach (var colis in colisToUpdate)
            {
                // Timeline
                await _timelineService.AddEventAsync(descriptionEvent, colisId: colis.Id, statut: newColisStatus.ToString());
                
                // Notif Client
                if (colis.Client?.UserAccount != null)
                {
                    string emoji = newColisStatus == StatutColis.Livre ? "✅" : "📦";
                    await _notificationService.CreateAndSendAsync(
                        title: $"{emoji} Suivi Colis",
                        message: $"Votre colis {colis.NumeroReference} est maintenant : {newColisStatus}",
                        userId: colis.Client.UserAccount.Id,
                        categorie: CategorieNotification.StatutColis,
                        actionUrl: $"/colis/edit/{colis.Id}",
                        entityId: colis.Id,
                        entityType: "Colis"
                    );
                }
            }

            // Pour les Véhicules
            foreach (var vehicule in vehiculesToUpdate)
            {
                // Timeline
                await _timelineService.AddEventAsync(descriptionEvent, vehiculeId: vehicule.Id, statut: newVehiculeStatus.ToString());
                
                // Notif Client
                if (vehicule.Client?.UserAccount != null)
                {
                    string emoji = newVehiculeStatus == StatutVehicule.Livre ? "✅" : "🚗";
                    await _notificationService.CreateAndSendAsync(
                        title: $"{emoji} Suivi Véhicule",
                        message: $"Votre véhicule {vehicule.Marque} ({vehicule.Immatriculation}) est maintenant : {newVehiculeStatus}",
                        userId: vehicule.Client.UserAccount.Id,
                        categorie: CategorieNotification.StatutVehicule,
                        actionUrl: $"/vehicule/edit/{vehicule.Id}",
                        entityId: vehicule.Id,
                        entityType: "Vehicule"
                    );
                }
            }
            // === FIN DE LA CORRECTION ===
        }

        // ... Reste du fichier inchangé (UpdatePlombOnChildren, DeleteAsync, etc.) ...
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
                throw new InvalidOperationException("Impossible de supprimer un conteneur qui n'est pas vide.");
            }
            conteneur.Actif = false;
            conteneur.Statut = StatutConteneur.Annule;
            await context.SaveChangesAsync();
            return true;
        }

        // ... (Autres méthodes de lecture inchangées) ...
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

        public Task<decimal> GetAverageFillingRateAsync() => Task.FromResult(0m);

        public async Task<IEnumerable<Conteneur>> GetUpcomingDeparturesAsync(int days)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var upcomingDate = DateTime.UtcNow.AddDays(days);
            return await context.Conteneurs
                .Where(c => c.Actif && c.DateDepart.HasValue && c.DateDepart.Value >= DateTime.UtcNow && c.DateDepart.Value <= upcomingDate)
                .OrderBy(c => c.DateDepart)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetAlmostFullContainersAsync(decimal threshold)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var activeStatuses = new[] { StatutConteneur.Reçu, StatutConteneur.EnPreparation };
            return await context.Conteneurs
                .Where(c => c.Actif && activeStatuses.Contains(c.Statut) && (c.Colis.Count() + c.Vehicules.Count()) >= (int)threshold)
                .Include(c => c.Colis)
                .Include(c => c.Vehicules)
                .ToListAsync();
        }

        public Task<Dictionary<string, int>> GetStatsByDestinationAsync() => Task.FromResult(new Dictionary<string, int>());
        public Task<bool> CanAddColisAsync(Guid conteneurId, Guid colisId) => throw new NotImplementedException();
        public Task<decimal> CalculateProfitabilityAsync(Guid conteneurId) => throw new NotImplementedException();
        
        public async Task<IEnumerable<Conteneur>> GetByClientIdAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis)
                .Include(c => c.Vehicules)
                .Where(c => c.Actif && (c.Colis.Any(col => col.ClientId == clientId && col.Actif) || c.Vehicules.Any(v => v.ClientId == clientId && v.Actif)))
                .OrderByDescending(c => c.DateCreation)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}