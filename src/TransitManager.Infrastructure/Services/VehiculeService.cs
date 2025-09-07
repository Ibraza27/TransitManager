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

        public VehiculeService(IDbContextFactory<TransitContext> contextFactory, IConteneurService conteneurService, IClientService clientService)
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

            if (vehicule.Statut == StatutVehicule.Retourne)
            {
                vehicule.ConteneurId = null;
            }

            context.Entry(vehiculeInDb).CurrentValues.SetValues(vehicule);
            vehiculeInDb.ClientId = vehicule.ClientId;
            vehiculeInDb.ConteneurId = vehicule.ConteneurId;

			// ======================= DÉBUT DE L'AJOUT (Concurrence) =======================
			context.Entry(vehiculeInDb).Property("RowVersion").OriginalValue = vehicule.RowVersion;
			// ======================== FIN DE L'AJOUT (Concurrence) ========================

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
	
			await _clientService.RecalculateAndUpdateClientStatisticsAsync(vehiculeInDb.ClientId);

            if (originalConteneurId.HasValue)
            {
                await _conteneurService.RecalculateStatusAsync(originalConteneurId.Value);
            }
            if(vehiculeInDb.ConteneurId.HasValue && vehiculeInDb.ConteneurId != originalConteneurId)
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
            // On ajoute le statut "Probleme" à la liste des statuts valides pour une affectation
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
				.Include(v => v.Paiements) // <-- LIGNE À AJOUTER
				.AsNoTracking()
				.FirstOrDefaultAsync(v => v.Id == id);
		}

        public async Task<IEnumerable<Vehicule>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Vehicules.Include(v => v.Client).Include(v => v.Conteneur).AsNoTracking().OrderByDescending(v => v.DateCreation).ToListAsync();
        }

        public async Task<IEnumerable<Vehicule>> GetByClientAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Vehicules.Include(v => v.Client).Where(v => v.ClientId == clientId).AsNoTracking().OrderByDescending(v => v.DateCreation).ToListAsync();
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
            var searchTermLower = searchTerm.ToLower();
            return await context.Vehicules.Include(v => v.Client).Where(v => v.Immatriculation.ToLower().Contains(searchTermLower) || v.Marque.ToLower().Contains(searchTermLower) || v.Modele.ToLower().Contains(searchTermLower) || (v.Commentaires != null && v.Commentaires.ToLower().Contains(searchTermLower)) || (v.Client != null && (v.Client.Nom + " " + v.Client.Prenom).ToLower().Contains(searchTermLower))).AsNoTracking().OrderByDescending(v => v.DateCreation).ToListAsync();
        }
    }
}