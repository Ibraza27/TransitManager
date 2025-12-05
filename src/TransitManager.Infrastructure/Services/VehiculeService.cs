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

        public VehiculeService(
            IDbContextFactory<TransitContext> contextFactory,
            IConteneurService conteneurService,
            IClientService clientService)
        {
            _contextFactory = contextFactory;
            _conteneurService = conteneurService;
            _clientService = clientService;
        }

        public async Task<Vehicule> CreateAsync(Vehicule vehicule)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Vehicules.Add(vehicule);
            await context.SaveChangesAsync();
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

			// --- MAPPAGE EXISTANT ---
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
			vehiculeInDb.Statut = vehicule.Statut;
			vehiculeInDb.ConteneurId = vehicule.ConteneurId;
			
			// Protection des données financières (si nécessaire)
			if (vehicule.PrixTotal > 0) vehiculeInDb.PrixTotal = vehicule.PrixTotal;
			if (vehicule.ValeurDeclaree > 0) vehiculeInDb.ValeurDeclaree = vehicule.ValeurDeclaree;
			// On garde la SommePayee à jour si elle est envoyée
			vehiculeInDb.SommePayee = vehicule.SommePayee;

			// --- CORRECTION : MAPPAGE DES NOUVEAUX CHAMPS (État des lieux & Signatures) ---
			// Si ces champs ne sont pas mis à jour ici, ils ne seront jamais sauvegardés !
			vehiculeInDb.EtatDesLieux = vehicule.EtatDesLieux;
			vehiculeInDb.EtatDesLieuxRayures = vehicule.EtatDesLieuxRayures;
			
			// Nouveaux champs ajoutés lors de la migration précédente
			vehiculeInDb.AccessoiresJson = vehicule.AccessoiresJson;
			vehiculeInDb.SignatureAgent = vehicule.SignatureAgent;
			vehiculeInDb.SignatureClient = vehicule.SignatureClient;
			vehiculeInDb.LieuEtatDesLieux = vehicule.LieuEtatDesLieux;
			vehiculeInDb.DateEtatDesLieux = vehicule.DateEtatDesLieux;
			// -----------------------------------------------------------------------------

			context.Entry(vehiculeInDb).Property("RowVersion").OriginalValue = vehicule.RowVersion;

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
					throw new ConcurrencyException("Ce véhicule a été supprimé par un autre utilisateur.");
				}
				else
				{
					throw new ConcurrencyException("Ce véhicule a été modifié par un autre utilisateur. Vos modifications n'ont pas pu être enregistrées.");
				}
			}

			// Appels aux services externes
			await _clientService.RecalculateAndUpdateClientStatisticsAsync(vehiculeInDb.ClientId);
			if (originalConteneurId.HasValue)
			{
				await _conteneurService.RecalculateStatusAsync(originalConteneurId.Value);
			}
			if (vehiculeInDb.ConteneurId.HasValue && vehiculeInDb.ConteneurId != originalConteneurId)
			{
				await _conteneurService.RecalculateStatusAsync(vehiculeInDb.ConteneurId.Value);
			}

			return vehiculeInDb;
		}


        public async Task<bool> AssignToConteneurAsync(Guid vehiculeId, Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vehicule = await context.Vehicules.FindAsync(vehiculeId);
            var conteneur = await context.Conteneurs.FindAsync(conteneurId);
            var canReceiveStatuses = new[] { StatutConteneur.Reçu, StatutConteneur.EnPreparation, StatutConteneur.Probleme };
            if (vehicule == null || conteneur == null || !canReceiveStatuses.Contains(conteneur.Statut)) return false;
            vehicule.ConteneurId = conteneurId;
            vehicule.Statut = StatutVehicule.Affecte;
            vehicule.NumeroPlomb = conteneur.NumeroPlomb;
            await context.SaveChangesAsync();
            await _conteneurService.RecalculateStatusAsync(conteneurId);
            return true;
        }

        public async Task<bool> RemoveFromConteneurAsync(Guid vehiculeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vehicule = await context.Vehicules.FindAsync(vehiculeId);
            if (vehicule == null || !vehicule.ConteneurId.HasValue) return false;
            var originalConteneurId = vehicule.ConteneurId.Value;
            vehicule.ConteneurId = null;
            vehicule.Statut = StatutVehicule.EnAttente;
            vehicule.NumeroPlomb = null;
            await context.SaveChangesAsync();
            await _conteneurService.RecalculateStatusAsync(originalConteneurId);
            return true;
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
			
			// 1. Trouver l'utilisateur pour avoir son ClientId
			var user = await context.Utilisateurs.FindAsync(userId);
			
			// Si l'utilisateur n'est pas lié à un client, il ne voit rien
			if (user == null || !user.ClientId.HasValue)
			{
				return Enumerable.Empty<Vehicule>();
			}

			// 2. Retourner les véhicules de ce client
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
