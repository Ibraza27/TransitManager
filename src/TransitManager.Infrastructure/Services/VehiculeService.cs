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
        private readonly TransitContext _context;
        private readonly IConteneurService _conteneurService;
        private readonly IClientService _clientService;

        public VehiculeService(TransitContext context, IConteneurService conteneurService, IClientService clientService)
        {
            _context = context;
            _conteneurService = conteneurService;
            _clientService = clientService;
        }

        public async Task<Vehicule> CreateAsync(Vehicule vehicule)
        {
            _context.Vehicules.Add(vehicule);
            await _context.SaveChangesAsync();
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(vehicule.ClientId);

            if (vehicule.ConteneurId.HasValue)
            {
                await _conteneurService.RecalculateStatusAsync(vehicule.ConteneurId.Value);
            }
            return vehicule;
        }

        public async Task<Vehicule> UpdateAsync(Vehicule vehicule)
        {
            var vehiculeInDb = await _context.Vehicules.FindAsync(vehicule.Id);
            if (vehiculeInDb == null)
            {
                throw new InvalidOperationException("Le véhicule que vous essayez de modifier n'existe plus.");
            }

            var originalConteneurId = vehiculeInDb.ConteneurId;
            if (vehicule.Statut == StatutVehicule.Retourne)
            {
                vehicule.ConteneurId = null;
            }
            _context.Entry(vehiculeInDb).CurrentValues.SetValues(vehicule);
            vehiculeInDb.ClientId = vehicule.ClientId;
            vehiculeInDb.ConteneurId = vehicule.ConteneurId;
            _context.Entry(vehiculeInDb).Property("RowVersion").OriginalValue = vehicule.RowVersion;
            try
            {
                await _context.SaveChangesAsync();
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
            if (vehiculeInDb.ConteneurId.HasValue && vehiculeInDb.ConteneurId != originalConteneurId)
            {
                await _conteneurService.RecalculateStatusAsync(vehiculeInDb.ConteneurId.Value);
            }
            return vehiculeInDb;
        }

        public async Task<bool> AssignToConteneurAsync(Guid vehiculeId, Guid conteneurId)
        {
            var vehicule = await _context.Vehicules.FindAsync(vehiculeId);
            var conteneur = await _context.Conteneurs.FindAsync(conteneurId);
            var canReceiveStatuses = new[] { StatutConteneur.Reçu, StatutConteneur.EnPreparation, StatutConteneur.Probleme };
            if (vehicule == null || conteneur == null || !canReceiveStatuses.Contains(conteneur.Statut)) return false;

            vehicule.ConteneurId = conteneurId;
            vehicule.Statut = StatutVehicule.Affecte;
            vehicule.NumeroPlomb = conteneur.NumeroPlomb;
            await _context.SaveChangesAsync();
            await _conteneurService.RecalculateStatusAsync(conteneurId);
            return true;
        }

        public async Task<bool> RemoveFromConteneurAsync(Guid vehiculeId)
        {
            var vehicule = await _context.Vehicules.FindAsync(vehiculeId);
            if (vehicule == null || !vehicule.ConteneurId.HasValue) return false;
            var originalConteneurId = vehicule.ConteneurId.Value;
            vehicule.ConteneurId = null;
            vehicule.Statut = StatutVehicule.EnAttente;
            vehicule.NumeroPlomb = null;
            await _context.SaveChangesAsync();
            await _conteneurService.RecalculateStatusAsync(originalConteneurId);
            return true;
        }

        public async Task<Vehicule?> GetByIdAsync(Guid id)
        {
            return await _context.Vehicules
                .Include(v => v.Client)
                .Include(v => v.Paiements)
                .Include(v => v.Conteneur)
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<IEnumerable<Vehicule>> GetAllAsync()
        {
            return await _context.Vehicules.Include(v => v.Client).Include(v => v.Conteneur).AsNoTracking().OrderByDescending(v => v.DateCreation).ToListAsync();
        }

        public async Task<IEnumerable<Vehicule>> GetByClientAsync(Guid clientId)
        {
            return await _context.Vehicules.Include(v => v.Client).Where(v => v.ClientId == clientId).AsNoTracking().OrderByDescending(v => v.DateCreation).ToListAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var vehicule = await _context.Vehicules.FindAsync(id);
            if (vehicule == null) return false;
            var clientId = vehicule.ClientId;
            vehicule.Actif = false;
            await _context.SaveChangesAsync();
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(clientId);
            return true;
        }

        public async Task<IEnumerable<Vehicule>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllAsync();
            var searchTerms = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var query = _context.Vehicules
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
            var vehicule = await _context.Vehicules.FirstOrDefaultAsync(v => v.Id == vehiculeId);
            if (vehicule != null)
            {
                var validStatuses = new[] { StatutPaiement.Paye, StatutPaiement.Valide };
                var totalPaye = await _context.Paiements
                    .Where(p => p.VehiculeId == vehiculeId &&
                                p.Actif &&
                                validStatuses.Contains(p.Statut))
                    .SumAsync(p => p.Montant);
                vehicule.SommePayee = totalPaye;
                await _context.SaveChangesAsync();

                await _clientService.RecalculateAndUpdateClientStatisticsAsync(vehicule.ClientId);
            }
        }
    }
}
