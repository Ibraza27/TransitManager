using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Repositories
{
    public interface IClientRepository : IGenericRepository<Client>
    {
        Task<Client?> GetByCodeAsync(string codeClient);
        Task<Client?> GetWithDetailsAsync(Guid id);
        Task<IEnumerable<Client>> GetActiveClientsAsync();
        Task<IEnumerable<Client>> SearchAsync(string searchTerm);
        Task<IEnumerable<Client>> GetFideleClientsAsync();
        Task<IEnumerable<Client>> GetClientsWithUnpaidBalanceAsync();
        Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, Guid? excludeId = null);
        Task<IEnumerable<string?>> GetAllCitiesAsync();
        Task<Dictionary<string, int>> GetClientsByLocationAsync();
    }

    public class ClientRepository : GenericRepository<Client>, IClientRepository
    {
        public ClientRepository(TransitContext context) : base(context)
        {
        }

        public async Task<Client?> GetByCodeAsync(string codeClient)
        {
            return await _dbSet
                .Include(c => c.Colis)
                .Include(c => c.Paiements)
                .FirstOrDefaultAsync(c => c.CodeClient == codeClient && c.Actif);
        }

        public async Task<Client?> GetWithDetailsAsync(Guid id)
        {
            return await _dbSet
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Conteneur)
                .Include(c => c.Paiements)
                .FirstOrDefaultAsync(c => c.Id == id && c.Actif);
        }

        public async Task<IEnumerable<Client>> GetActiveClientsAsync()
        {
            return await _dbSet
                .Where(c => c.Actif)
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveClientsAsync();

            searchTerm = searchTerm.ToLower();

            return await _dbSet
                .Where(c => c.Actif && (
                    c.CodeClient.ToLower().Contains(searchTerm) ||
                    c.Nom.ToLower().Contains(searchTerm) ||
                    c.Prenom.ToLower().Contains(searchTerm) ||
                    (c.Nom + " " + c.Prenom).ToLower().Contains(searchTerm) ||
                    c.TelephonePrincipal.Contains(searchTerm) ||
                    (c.TelephoneSecondaire != null && c.TelephoneSecondaire.Contains(searchTerm)) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                    (c.Ville != null && c.Ville.ToLower().Contains(searchTerm)) ||
                    (c.AdressePrincipale != null && c.AdressePrincipale.ToLower().Contains(searchTerm))
                ))
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetFideleClientsAsync()
        {
            return await _dbSet
                .Where(c => c.Actif && c.EstClientFidele)
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

		public async Task<IEnumerable<Client>> GetClientsWithUnpaidBalanceAsync()
		{
			return await _dbSet
				.Where(c => c.Actif && c.Impayes > 0) // MODIFIÉ
				.OrderByDescending(c => c.Impayes) // MODIFIÉ
				.ToListAsync();
		}

        public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true;

            var query = _dbSet.Where(c => c.Email == email && c.Actif);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return true;

            var query = _dbSet.Where(c => c.TelephonePrincipal == phone && c.Actif);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<IEnumerable<string?>> GetAllCitiesAsync()
        {
            return await _dbSet
                .Where(c => c.Actif && c.Ville != null)
                .Select(c => c.Ville)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetClientsByLocationAsync()
        {
            return await _dbSet
                .Where(c => c.Actif && c.Ville != null)
                .GroupBy(c => c.Ville!)
                .Select(g => new { Ville = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Ville, x => x.Count);
        }
    }
}