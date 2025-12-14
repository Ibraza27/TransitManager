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
    public class VehiculeService : IVehiculeService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly IConteneurService _conteneurService;
        private readonly IClientService _clientService;
        private readonly ITimelineService _timelineService;
        private readonly INotificationService _notificationService;
        private readonly IAuthenticationService _authService;

        // Statuts qui ne doivent pas être écrasés par le conteneur
        private readonly StatutVehicule[] _statutsCritiques = new[]
        {
            StatutVehicule.Livre,
            StatutVehicule.Retourne,
            StatutVehicule.Probleme
        };

        public VehiculeService(
            IDbContextFactory<TransitContext> contextFactory,
            IConteneurService conteneurService,
            IClientService clientService,
            ITimelineService timelineService,
            INotificationService notificationService,
            IAuthenticationService authService)
        {
            _contextFactory = contextFactory;
            _conteneurService = conteneurService;
            _clientService = clientService;
            _timelineService = timelineService;
            _notificationService = notificationService;
            _authService = authService;
        }

        public async Task<Vehicule> CreateAsync(Vehicule vehicule)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Si un conteneur est assigné à la création, on applique la logique de statut
            if (vehicule.ConteneurId.HasValue)
            {
                var conteneur = await context.Conteneurs.FindAsync(vehicule.ConteneurId.Value);
                if (conteneur != null)
                {
                    vehicule.Statut = GetStatutFromConteneur(conteneur.Statut);
                    vehicule.NumeroPlomb = conteneur.NumeroPlomb;
                }
            }

            context.Vehicules.Add(vehicule);
            await context.SaveChangesAsync();

            // Timeline Création
            await _timelineService.AddEventAsync(
                "Véhicule créé et enregistré",
                vehiculeId: vehicule.Id,
                statut: vehicule.Statut.ToString()
            );

            // Notification création (Pour le Client si créé par Admin)
            var currentUser = _authService.CurrentUser;
            bool isAdmin = currentUser?.Role == RoleUtilisateur.Administrateur || currentUser?.Role == RoleUtilisateur.Gestionnaire;

            if (isAdmin)
            {
                var clientUser = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == vehicule.ClientId);
                if (clientUser != null)
                {
                    await _notificationService.CreateAndSendAsync(
                        title: "🚗 Nouveau Véhicule",
                        message: $"Le véhicule {vehicule.Marque} {vehicule.Modele} ({vehicule.Immatriculation}) a été ajouté à votre dossier.",
                        userId: clientUser.Id,
                        categorie: CategorieNotification.StatutVehicule,
                        actionUrl: $"/vehicule/edit/{vehicule.Id}",
                        entityId: vehicule.Id,
                        entityType: "Vehicule"
                    );
                }
            }

            // Notification aux Admins (si créé par un autre admin ou via un import)
            await _notificationService.CreateAndSendAsync(
                title: "🚗 Nouveau Véhicule",
                message: $"Nouveau véhicule ajouté : {vehicule.Immatriculation} (Client : {vehicule.ClientId})",
                userId: null, // Broadcast Admin
                categorie: CategorieNotification.StatutVehicule,
                actionUrl: $"/vehicule/edit/{vehicule.Id}",
                entityId: vehicule.Id,
                entityType: "Vehicule"
            );

            await _clientService.RecalculateAndUpdateClientStatisticsAsync(vehicule.ClientId);
            if (vehicule.ConteneurId.HasValue)
            {
                await _conteneurService.RecalculateStatusAsync(vehicule.ConteneurId.Value);
            }
            return vehicule;
        }

        public async Task<Vehicule> UpdateAsync(Vehicule vehicule)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vehiculeInDb = await context.Vehicules.FindAsync(vehicule.Id);

            if (vehiculeInDb == null)
            {
                throw new InvalidOperationException("Le véhicule que vous essayez de modifier n'existe plus.");
            }

            var originalConteneurId = vehiculeInDb.ConteneurId;
            var oldStatus = vehiculeInDb.Statut;

            // --- 1. Logique "Retourné" ---
            if (vehicule.Statut == StatutVehicule.Retourne)
            {
                vehicule.ConteneurId = null;
            }

            // --- 2. Mappage des champs ---
            vehiculeInDb.Immatriculation = vehicule.Immatriculation;
            vehiculeInDb.Marque = vehicule.Marque;
            vehiculeInDb.Modele = vehicule.Modele;
            vehiculeInDb.Annee = vehicule.Annee;
            vehiculeInDb.Kilometrage = vehicule.Kilometrage;
            vehiculeInDb.DestinationFinale = vehicule.DestinationFinale;
            vehiculeInDb.Destinataire = vehicule.Destinataire;
            vehiculeInDb.TelephoneDestinataire = vehicule.TelephoneDestinataire;
            vehiculeInDb.Type = vehicule.Type;
            vehiculeInDb.Commentaires = vehicule.Commentaires;
            vehiculeInDb.ConteneurId = vehicule.ConteneurId;

            // Protection financière
            if (vehicule.PrixTotal > 0) vehiculeInDb.PrixTotal = vehicule.PrixTotal;
            if (vehicule.ValeurDeclaree > 0) vehiculeInDb.ValeurDeclaree = vehicule.ValeurDeclaree;
            vehiculeInDb.SommePayee = vehicule.SommePayee;

            // État des lieux
            vehiculeInDb.EtatDesLieux = vehicule.EtatDesLieux;
            vehiculeInDb.EtatDesLieuxRayures = vehicule.EtatDesLieuxRayures;
            vehiculeInDb.AccessoiresJson = vehicule.AccessoiresJson;
            vehiculeInDb.SignatureAgent = vehicule.SignatureAgent;
            vehiculeInDb.SignatureClient = vehicule.SignatureClient;
            vehiculeInDb.LieuEtatDesLieux = vehicule.LieuEtatDesLieux;
            vehiculeInDb.DateEtatDesLieux = vehicule.DateEtatDesLieux;

            // --- 3. Logique Statut ---
            if (_statutsCritiques.Contains(vehicule.Statut))
            {
                vehiculeInDb.Statut = vehicule.Statut;
            }
            else if (vehiculeInDb.ConteneurId.HasValue)
            {
                var conteneur = await context.Conteneurs.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == vehiculeInDb.ConteneurId.Value);

                if (conteneur != null)
                {
                    vehiculeInDb.Statut = GetStatutFromConteneur(conteneur.Statut);
                    vehiculeInDb.NumeroPlomb = conteneur.NumeroPlomb;
                }
                else
                {
                    vehiculeInDb.Statut = vehicule.Statut;
                }
            }
            else
            {
                vehiculeInDb.Statut = vehicule.Statut;
                vehiculeInDb.NumeroPlomb = null;
            }

            if (vehicule.ConteneurId == null)
            {
                vehiculeInDb.NumeroPlomb = null;
            }

            context.Entry(vehiculeInDb).Property("RowVersion").OriginalValue = vehicule.RowVersion;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("Ce véhicule a été modifié par un autre utilisateur.");
            }

            // Notification Changement de Statut
            if (oldStatus != vehiculeInDb.Statut)
            {
                // Timeline
                await _timelineService.AddEventAsync(
                    $"Statut modifié : {oldStatus} -> {vehiculeInDb.Statut}",
                    vehiculeId: vehiculeInDb.Id,
                    statut: vehiculeInDb.Statut.ToString()
                );

                // Notifier le Client
                var clientUser = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == vehiculeInDb.ClientId);
                if (clientUser != null)
                {
                    string emoji = vehiculeInDb.Statut == StatutVehicule.Livre ? "✅" : "🚗";
                    await _notificationService.CreateAndSendAsync(
                        title: $"{emoji} Mise à jour Véhicule",
                        message: $"Votre véhicule {vehiculeInDb.Marque} ({vehiculeInDb.Immatriculation}) est maintenant : {vehiculeInDb.Statut}",
                        userId: clientUser.Id,
                        categorie: CategorieNotification.StatutVehicule,
                        actionUrl: $"/vehicule/edit/{vehiculeInDb.Id}",
                        entityId: vehiculeInDb.Id,
                        entityType: "Vehicule"
                    );
                }
            }

            // Services externes
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(vehiculeInDb.ClientId);
            if (originalConteneurId.HasValue) await _conteneurService.RecalculateStatusAsync(originalConteneurId.Value);
            if (vehiculeInDb.ConteneurId.HasValue && vehiculeInDb.ConteneurId != originalConteneurId) await _conteneurService.RecalculateStatusAsync(vehiculeInDb.ConteneurId.Value);

            return vehiculeInDb;
        }

        public async Task<bool> AssignToConteneurAsync(Guid vehiculeId, Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vehicule = await context.Vehicules.FindAsync(vehiculeId);
            var conteneur = await context.Conteneurs.FindAsync(conteneurId);

            var blockedStatuses = new[] { StatutConteneur.Cloture, StatutConteneur.Annule };
            if (vehicule == null || conteneur == null || blockedStatuses.Contains(conteneur.Statut)) return false;

            var oldStatus = vehicule.Statut;

            vehicule.ConteneurId = conteneurId;
            vehicule.NumeroPlomb = conteneur.NumeroPlomb;

            if (!_statutsCritiques.Contains(vehicule.Statut))
            {
                vehicule.Statut = GetStatutFromConteneur(conteneur.Statut);
            }

            await context.SaveChangesAsync();

            // Timeline
            if (oldStatus != vehicule.Statut)
            {
                await _timelineService.AddEventAsync(
                    $"Mise à jour automatique (Conteneur {conteneur.Statut}) : {oldStatus} -> {vehicule.Statut}",
                    vehiculeId: vehicule.Id,
                    statut: vehicule.Statut.ToString());
            }

            await _timelineService.AddEventAsync(
                $"Affecté au conteneur {conteneur.NumeroDossier}",
                vehiculeId: vehicule.Id,
                conteneurId: conteneur.Id,
                location: conteneur.NomCompagnie,
                statut: vehicule.Statut.ToString()
            );

            await _conteneurService.RecalculateStatusAsync(conteneurId);
            return true;
        }

        public async Task<bool> RemoveFromConteneurAsync(Guid vehiculeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vehicule = await context.Vehicules.FindAsync(vehiculeId);
            if (vehicule == null || !vehicule.ConteneurId.HasValue) return false;

            var originalConteneurId = vehicule.ConteneurId.Value;
            var oldStatus = vehicule.Statut;

            vehicule.ConteneurId = null;
            vehicule.NumeroPlomb = null;

            if (!_statutsCritiques.Contains(vehicule.Statut))
            {
                vehicule.Statut = StatutVehicule.EnAttente;
            }

            await context.SaveChangesAsync();

            if (oldStatus != vehicule.Statut)
            {
                await _timelineService.AddEventAsync(
                    $"Retrait conteneur : {oldStatus} -> {vehicule.Statut}",
                    vehiculeId: vehicule.Id,
                    statut: vehicule.Statut.ToString());
            }

            await _conteneurService.RecalculateStatusAsync(originalConteneurId);
            return true;
        }

        private StatutVehicule GetStatutFromConteneur(StatutConteneur statutConteneur)
        {
            return statutConteneur switch
            {
                StatutConteneur.EnTransit => StatutVehicule.EnTransit,
                StatutConteneur.Arrive => StatutVehicule.Arrive,
                StatutConteneur.EnDedouanement => StatutVehicule.EnDedouanement,
                StatutConteneur.Livre => StatutVehicule.Livre,
                _ => StatutVehicule.Affecte
            };
        }

        public async Task<Vehicule?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Vehicules
                .Include(v => v.Client)
                .Include(v => v.Paiements)
                .Include(v => v.Conteneur)
                .Include(v => v.Documents)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<IEnumerable<Vehicule>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Vehicules
                .Include(v => v.Client)
                .Include(v => v.Conteneur)
                .AsNoTracking()
                .OrderByDescending(v => v.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Vehicule>> GetByClientAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Vehicules
                .Include(v => v.Client)
                .Where(v => v.ClientId == clientId)
                .AsNoTracking()
                .OrderByDescending(v => v.DateCreation)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vehicule = await context.Vehicules.FindAsync(id);
            if (vehicule == null) return false;
            var clientId = vehicule.ClientId;
            vehicule.Actif = false;
            await context.SaveChangesAsync();
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(clientId);
            return true;
        }

        public async Task<IEnumerable<Vehicule>> SearchAsync(string searchTerm)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllAsync();
            var searchTerms = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var query = context.Vehicules
                               .Include(v => v.Client)
                               .Include(v => v.Conteneur)
                               .AsNoTracking();
            foreach (var term in searchTerms)
            {
                query = query.Where(v =>
                    EF.Functions.ILike(v.Immatriculation, $"%{term}%") ||
                    EF.Functions.ILike(v.Marque, $"%{term}%") ||
                    EF.Functions.ILike(v.Modele, $"%{term}%") ||
                    (v.Client != null && EF.Functions.ILike(v.Client.NomComplet, $"%{term}%")) ||
                    (v.Annee.ToString() == term) ||
                    (v.Commentaires != null && EF.Functions.ILike(v.Commentaires, $"%{term}%"))
                );
            }
            return await query.OrderByDescending(v => v.DateCreation).ToListAsync();
        }

        public async Task RecalculateAndUpdateVehiculeStatisticsAsync(Guid vehiculeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vehicule = await context.Vehicules.FirstOrDefaultAsync(v => v.Id == vehiculeId);
            if (vehicule != null)
            {
                var validStatuses = new[] { StatutPaiement.Paye, StatutPaiement.Valide };
                var totalPaye = await context.Paiements
                    .Where(p => p.VehiculeId == vehiculeId &&
                                p.Actif &&
                                validStatuses.Contains(p.Statut))
                    .SumAsync(p => p.Montant);
                vehicule.SommePayee = totalPaye;
                await context.SaveChangesAsync();
                await _clientService.RecalculateAndUpdateClientStatisticsAsync(vehicule.ClientId);
            }
        }

        public async Task<IEnumerable<Vehicule>> GetByUserIdAsync(Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FindAsync(userId);
            if (user == null || !user.ClientId.HasValue) return Enumerable.Empty<Vehicule>();

            return await context.Vehicules
                .Include(v => v.Client)
                .Include(v => v.Conteneur)
                .Where(v => v.ClientId == user.ClientId.Value && v.Actif)
                .OrderByDescending(v => v.DateCreation)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
