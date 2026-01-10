using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly TransitContext _context;

        public AuditService(TransitContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(int page, int pageSize, string? userId = null, string? entityName = null, DateTime? date = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var userGuid))
            {
                query = query.Where(a => a.UtilisateurId == userGuid);
            }

            if (!string.IsNullOrWhiteSpace(entityName))
            {
                query = query.Where(a => a.Entite == entityName);
            }

            if (date.HasValue)
            {
                var dayStart = date.Value.Date;
                var dayEnd = dayStart.AddDays(1);
                query = query.Where(a => a.DateAction >= dayStart && a.DateAction < dayEnd);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(a => a.DateAction)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDto
                {
                    Id = a.Id,
                    UserId = a.UtilisateurId.ToString(),
                    // Join simulation (or actual join if nav prop exists, existing entity has virtual Utilisateur?)
                    // The existing entity has `public virtual Utilisateur? Utilisateur { get; set; }`.
                    // So we can use a.Utilisateur.Prenom + " " + a.Utilisateur.Nom
                    UserName = a.Utilisateur != null 
                        ? (a.Utilisateur.Prenom + " " + a.Utilisateur.Nom).Trim() 
                        : "System",
                    Action = a.Action,
                    EntityName = a.Entite,
                    EntityId = a.EntiteId,
                    Timestamp = a.DateAction,
                    Description = $"{a.Action} {a.Entite} ({a.EntiteId})",
                    // We don't load JSON in list view to save bandwidth, usually. 
                    // But for now, let's load it or leave it null if list.
                    // Let's leave null for list and load in Detail.
                })
                .ToListAsync();

            return new PagedResult<AuditLogDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id)
        {
            var log = await _context.AuditLogs
                .Include(a => a.Utilisateur) // Eager load user
                .FirstOrDefaultAsync(a => a.Id == id);

            if (log == null) return null;

            return new AuditLogDto
            {
                Id = log.Id,
                UserId = log.UtilisateurId.ToString(),
                UserName = log.Utilisateur != null 
                    ? (log.Utilisateur.Prenom + " " + log.Utilisateur.Nom).Trim() 
                    : "System",
                Action = log.Action,
                EntityName = log.Entite,
                EntityId = log.EntiteId,
                Timestamp = log.DateAction,
                ValuesBefore = log.ValeurAvant,
                ValuesAfter = log.ValeurApres,
                Description = log.Commentaires ?? $"{log.Action} {log.Entite}"
            };
        }
    }
}
