using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.DTOs; // AJOUT
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
        Task<Core.DTOs.PagedResult<Client>> GetPagedAsync(int page, int pageSize, string? search = null);
    }

    public class ClientRepository : GenericRepository<Client>, IClientRepository
    {
        public ClientRepository(TransitContext context) : base(context)
        {
        }

        public async Task<Client?> GetByCodeAsync(string codeClient)
        {
            return await _context.Set<Client>()
                .Include(c => c.Colis)
                .Include(c => c.Paiements)
                .FirstOrDefaultAsync(c => c.CodeClient == codeClient && c.Actif);
        }

        public async Task<Client?> GetWithDetailsAsync(Guid id)
        {
            return await _context.Set<Client>()
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Conteneur)
                .Include(c => c.Vehicules)
                .Include(c => c.Paiements)
                .Include(c => c.UserAccount)
                .FirstOrDefaultAsync(c => c.Id == id && c.Actif);
        }

        public async Task<IEnumerable<Client>> GetActiveClientsAsync()
        {
            var clients = await _context.Set<Client>()
                .Include(c => c.Colis)
                .Include(c => c.Vehicules)
                .Where(c => c.Actif)
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();

            // Recalcul des impayés à la volée pour garantir la cohérence
            foreach (var client in clients)
            {
                var impayesColis = client.Colis
                    .Where(c => c.Actif)
                    .Sum(c => c.PrixTotal - c.SommePayee); // Utilisation directe du calcul si RestantAPayer n'est pas mappé

                var impayesVehicules = client.Vehicules
                    .Where(v => v.Actif)
                    .Sum(v => v.PrixTotal - v.SommePayee);

                client.Impayes = impayesColis + impayesVehicules;
            }

            return clients;
        }

        public async Task<IEnumerable<Client>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveClientsAsync();
            searchTerm = searchTerm.ToLower();
            return await _context.Set<Client>()
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
            return await _context.Set<Client>()
                .Where(c => c.Actif && c.EstClientFidele)
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetClientsWithUnpaidBalanceAsync()
        {
            return await _context.Set<Client>()
                .Where(c => c.Actif && c.Impayes > 0)
                .OrderByDescending(c => c.Impayes)
                .ToListAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return true;
            var query = _context.Set<Client>().Where(c => c.Email == email && c.Actif);
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
            var query = _context.Set<Client>().Where(c => c.TelephonePrincipal == phone && c.Actif);
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }
            return !await query.AnyAsync();
        }

        public async Task<IEnumerable<string?>> GetAllCitiesAsync()
        {
            return await _context.Set<Client>()
                .Where(c => c.Actif && c.Ville != null)
                .Select(c => c.Ville)
                .Distinct()
                .OrderBy(v => v)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetClientsByLocationAsync()
        {
            return await _context.Set<Client>()
                .Where(c => c.Actif && c.Ville != null)
                .GroupBy(c => c.Ville!)
                .Select(g => new { Ville = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Ville, x => x.Count);
        }

        public async Task<Core.DTOs.PagedResult<Client>> GetPagedAsync(int page, int pageSize, string? search = null)
        {
            var query = _context.Set<Client>().Where(c => c.Actif);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c => 
                    c.Nom.ToLower().Contains(search) || 
                    c.Prenom.ToLower().Contains(search) ||
                    (c.Nom + " " + c.Prenom).ToLower().Contains(search) ||
                    c.Email.ToLower().Contains(search) ||
                    c.TelephonePrincipal.Contains(search) ||
                    c.CodeClient.ToLower().Contains(search)
                );
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new Core.DTOs.PagedResult<Client>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }
    }
}
