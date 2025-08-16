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
    public class VehiculeService : IVehiculeService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;

        public VehiculeService(IDbContextFactory<TransitContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Vehicule> CreateAsync(Vehicule vehicule)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            // On attache le client existant au contexte pour éviter les conflits de tracking
            if (vehicule.Client != null)
            {
                context.Entry(vehicule.Client).State = EntityState.Unchanged;
            }
            context.Vehicules.Add(vehicule);
            await context.SaveChangesAsync();
            return vehicule;
        }

        public async Task<Vehicule> UpdateAsync(Vehicule vehicule)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vehiculeInDb = await context.Vehicules.FindAsync(vehicule.Id);
            if (vehiculeInDb == null) throw new Exception("Véhicule non trouvé pour la mise à jour.");
            
            // Copier les propriétés modifiées depuis l'objet de l'UI vers l'objet suivi par EF
            context.Entry(vehiculeInDb).CurrentValues.SetValues(vehicule);
            
            // S'assurer que le client n'est pas tracké à nouveau
            vehiculeInDb.ClientId = vehicule.ClientId;

            await context.SaveChangesAsync();
            return vehiculeInDb;
        }

        public async Task<bool> RemoveFromConteneurAsync(Guid vehiculeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var vehicule = await context.Vehicules.FindAsync(vehiculeId);
            if (vehicule == null) return false;
            vehicule.ConteneurId = null;
            vehicule.Statut = StatutVehicule.EnAttente;
            vehicule.NumeroPlomb = null;
            await context.SaveChangesAsync();
            return true;
        }

        // Le reste des méthodes ne change pas
        #region Méthodes de lecture et autres
        public async Task<Vehicule?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Vehicules.Include(v => v.Client).AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
        }
        public async Task<IEnumerable<Vehicule>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Vehicules.Include(v => v.Client).AsNoTracking().OrderByDescending(v => v.DateCreation).ToListAsync();
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
            vehicule.Actif = false;
            await context.SaveChangesAsync();
            return true;
        }
        public async Task<IEnumerable<Vehicule>> SearchAsync(string searchTerm)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllAsync();
            var searchTermLower = searchTerm.ToLower();
            return await context.Vehicules.Include(v => v.Client).Where(v => v.Immatriculation.ToLower().Contains(searchTermLower) || v.Marque.ToLower().Contains(searchTermLower) || v.Modele.ToLower().Contains(searchTermLower) || (v.Commentaires != null && v.Commentaires.ToLower().Contains(searchTermLower)) || (v.Client != null && (v.Client.Nom + " " + v.Client.Prenom).ToLower().Contains(searchTermLower))).AsNoTracking().OrderByDescending(v => v.DateCreation).ToListAsync();
        }
        #endregion
    }
}