using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Infrastructure.Data;
using TransitManager.Core.DTOs;

namespace TransitManager.Infrastructure.Repositories
{
    public interface IColisRepository : IGenericRepository<Colis>
    {
        Task<Colis?> GetByBarcodeAsync(string barcode);
        Task<Colis?> GetByReferenceAsync(string reference);
        Task<Colis?> GetWithDetailsAsync(Guid id);
        Task<IEnumerable<Colis>> GetByClientAsync(Guid clientId);
        Task<IEnumerable<Colis>> GetByConteneurAsync(Guid conteneurId);
        Task<IEnumerable<Colis>> GetByStatusAsync(StatutColis statut);
        Task<IEnumerable<Colis>> GetUnassignedAsync();
        Task<IEnumerable<Colis>> GetRecentAsync(int count);
        Task<IEnumerable<Colis>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<StatutColis, int>> GetStatisticsByStatusAsync();
        Task<IEnumerable<Colis>> SearchAsync(string searchTerm);
		Task<int> GetCountByStatusAsync(StatutColis statut);
		Task<IEnumerable<Colis>> GetColisWaitingLongTimeAsync(int days);
        Task<PagedResult<ColisListItemDto>> GetPagedAsync(int page, int pageSize, string? search = null, Guid? clientId = null);
    }

    public class ColisRepository : GenericRepository<Colis>, IColisRepository
    {
		
		public async Task<int> GetCountByStatusAsync(StatutColis statut)
		{
			return await _context.Colis.CountAsync(c => c.Statut == statut && c.Actif);
		}

		public async Task<IEnumerable<Colis>> GetColisWaitingLongTimeAsync(int days)
		{
			var dateLimit = DateTime.UtcNow.AddDays(-days);
			return await _context.Colis
				.Include(c => c.Client)
				.Where(c => c.Actif && c.Statut == StatutColis.EnAttente && c.DateArrivee < dateLimit)
				.OrderBy(c => c.DateArrivee)
				.ToListAsync();
		}
		
        public ColisRepository(TransitContext context) : base(context)
        {
        }

        public override async Task<IEnumerable<Colis>> GetAllAsync()
        {
             return await _context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ
                .Where(c => c.Actif)
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        // In GetByBarcodeAsync
        public async Task<Colis?> GetByBarcodeAsync(string barcode)
        {
            return await _context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes) // AJOUTÉ
                .FirstOrDefaultAsync(c => c.Barcodes.Any(b => b.Value == barcode) && c.Actif);
        }

        public async Task<Colis?> GetByReferenceAsync(string reference)
        {
            return await _context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ
                .FirstOrDefaultAsync(c => c.NumeroReference == reference && c.Actif);
        }

        public async Task<Colis?> GetWithDetailsAsync(Guid id)
        {
            return await _context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ
                .FirstOrDefaultAsync(c => c.Id == id && c.Actif);
        }

        public async Task<IEnumerable<Colis>> GetByClientAsync(Guid clientId)
        {
            return await _context.Set<Colis>()
                .Include(c => c.Client) // Already included, but good to be explicit
                .Include(c => c.Conteneur)
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ
                .Where(c => c.ClientId == clientId && c.Actif)
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByConteneurAsync(Guid conteneurId)
        {
            return await _context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur) // Already included, but good to be explicit
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ
                .Where(c => c.ConteneurId == conteneurId && c.Actif)
                .OrderBy(c => c.Client!.Nom)
                .ThenBy(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByStatusAsync(StatutColis statut)
        {
            return await _context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ
                .Where(c => c.Statut == statut && c.Actif)
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetUnassignedAsync()
        {
            return await _context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ
                .Where(c => c.ConteneurId == null && c.Actif && c.Statut == StatutColis.EnAttente)
                .OrderBy(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetRecentAsync(int count)
        {
            return await _context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ
                .Where(c => c.Actif)
                .OrderByDescending(c => c.DateArrivee)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Set<Colis>()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ
                .Where(c => c.DateArrivee >= startDate && c.DateArrivee <= endDate && c.Actif)
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<Dictionary<StatutColis, int>> GetStatisticsByStatusAsync()
        {
            return await _context.Colis
                .Where(c => c.Actif)
                .GroupBy(c => c.Statut)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);
        }

        public async Task<IEnumerable<Colis>> SearchAsync(string searchTerm)
        {
            searchTerm = searchTerm.ToLower();
            return await _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Documents)
                .Include(c => c.Barcodes)
                .Where(c => c.Actif && (
                    c.NumeroReference.ToLower().Contains(searchTerm) ||
                    c.Designation.ToLower().Contains(searchTerm) ||
                    (c.Client != null && (c.Client.Nom.ToLower().Contains(searchTerm) || c.Client.Prenom.ToLower().Contains(searchTerm))) ||
                    (c.Conteneur != null && c.Conteneur.NumeroDossier.ToLower().Contains(searchTerm)) ||
                    c.Barcodes.Any(b => b.Value.ToLower().Contains(searchTerm))
                ))
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<PagedResult<ColisListItemDto>> GetPagedAsync(int page, int pageSize, string? search = null, Guid? clientId = null)
        {
            var query = _context.Colis
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Documents) // AJOUTÉ
                .Include(c => c.Barcodes) // AJOUTÉ (Important pour le search si on filtrait en mémoire, mais ici c'est en base)
                .Where(c => c.Actif)
                .AsQueryable();

            if (clientId.HasValue)
            {
                query = query.Where(c => c.ClientId == clientId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();
                query = query.Where(c => 
                    c.NumeroReference.ToLower().Contains(search) || 
                    c.Designation.ToLower().Contains(search) ||
                    (c.Client != null && (c.Client.Nom.ToLower().Contains(search) || c.Client.Prenom.ToLower().Contains(search))) ||
                    (c.Conteneur != null && c.Conteneur.NumeroDossier.ToLower().Contains(search))
                    // TODO: Si on veut chercher par barcode ici, il faut l'ajouter au Where
                );
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.DateArrivee)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ColisListItemDto
                {
                    Id = c.Id,
                    NumeroReference = c.NumeroReference,
                    DateArrivee = c.DateArrivee,
                    ClientNomComplet = c.Client != null ? c.Client.Nom + " " + c.Client.Prenom : "Inconnu",
                    Statut = c.Statut,
                    DestinationFinale = c.DestinationFinale,
                    ConteneurId = c.ConteneurId,
                    ConteneurNumero = c.Conteneur != null ? c.Conteneur.NumeroDossier : null,
                    NombrePieces = c.NombrePieces,
                    Volume = c.Volume,
                    HasMissingDocuments = c.Documents.Any(d => d.Statut == StatutDocument.Manquant),
                    AllBarcodes = string.Join(", ", c.Barcodes.Select(b => b.Value)) // AJOUTÉ
                })
                .ToListAsync();

            return new PagedResult<ColisListItemDto>
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
