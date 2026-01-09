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
            // OPTIMISATION: Projection SQL directe pour éviter le N+1 et le chargement de toutes les collections
            var data = await _context.Set<Client>()
                .Where(c => c.Actif)
                .Select(c => new 
                { 
                    Client = c,
                    // Calcul direct en base de données
                    ImpayesColis = c.Colis.Where(x => x.Actif).Sum(x => x.PrixTotal - x.SommePayee),
                    ImpayesVehicules = c.Vehicules.Where(v => v.Actif)
                        // Logique inline de l'assurance pour le calcul précis
                        .Sum(v => (v.HasAssurance 
                            ? v.PrixTotal + ((((v.ValeurDeclaree + v.PrixTotal) * 1.2m * 0.007m) + 50m) < 250m ? 250m : (((v.ValeurDeclaree + v.PrixTotal) * 1.2m * 0.007m) + 50m))
                            : v.PrixTotal) - v.SommePayee)
                })
                .OrderBy(x => x.Client.Nom)
                .ThenBy(x => x.Client.Prenom)
                .AsNoTracking() // Gain de perf supplémentaire lecture seule
                .ToListAsync();

            // Matérialisation
            var results = new List<Client>();
            foreach (var item in data)
            {
                var c = item.Client;
                c.Impayes = item.ImpayesColis + item.ImpayesVehicules; // Affectation de la propriété non-mappée
                results.Add(c);
            }

            return results;
        }

        public async Task<IEnumerable<Client>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveClientsAsync();

            var term = $"%{searchTerm}%"; // Wildcards pour ILike

            return await _context.Set<Client>()
                .Where(c => c.Actif && (
                    EF.Functions.ILike(c.CodeClient, term) ||
                    EF.Functions.ILike(c.Nom, term) ||
                    EF.Functions.ILike(c.Prenom, term) ||
                    // Concaténation pour Nom complet (Note: ILike sur concat peut ne pas utiliser l'index, mais mieux que ToLower)
                    EF.Functions.ILike(c.Nom + " " + c.Prenom, term) || 
                    EF.Functions.ILike(c.TelephonePrincipal, term) ||
                    (c.TelephoneSecondaire != null && EF.Functions.ILike(c.TelephoneSecondaire, term)) ||
                    (c.Email != null && EF.Functions.ILike(c.Email, term)) ||
                    (c.Ville != null && EF.Functions.ILike(c.Ville, term)) ||
                    (c.AdressePrincipale != null && EF.Functions.ILike(c.AdressePrincipale, term))
                ))
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .AsNoTracking()
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
