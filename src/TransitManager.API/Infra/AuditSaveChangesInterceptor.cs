using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TransitManager.Core.Entities;

namespace TransitManager.API.Infra
{
    public class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Audit(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Audit(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void Audit(DbContext? context)
        {
            if (context == null) return;

            context.ChangeTracker.DetectChanges();

            var auditEntries = context.ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity && 
                            !(e.Entity is AuditLog) && // Avoid infinite loop
                            (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
                .ToList();

            if (!auditEntries.Any()) return;

            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;

            foreach (var entry in auditEntries)
            {
                var action = entry.State switch
                {
                    EntityState.Added => "CREATE",
                    EntityState.Modified => "UPDATE",
                    EntityState.Deleted => "DELETE",
                    _ => "UNKNOWN"
                };

                // Create Audit Log
                var audit = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    DateAction = now,
                    Action = action,
                    Entite = entry.Entity.GetType().Name,
                    UtilisateurId = userId,
                    AdresseIP = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString()
                };

                // Get Primary Key (First property named "Id" or defined as Key)
                var primaryKey = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                audit.EntiteId = primaryKey?.CurrentValue?.ToString();

                // Serialize Values
                if (entry.State != EntityState.Added)
                {
                    var originalValues = entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()).ToDictionary(p => p.Metadata.Name, p => p.OriginalValue);
                    audit.ValeurAvant = System.Text.Json.JsonSerializer.Serialize(originalValues);
                }

                if (entry.State != EntityState.Deleted)
                {
                    var currentValues = entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()).ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
                    audit.ValeurApres = System.Text.Json.JsonSerializer.Serialize(currentValues);
                }

                // Metadata update on the entity itself (BaseEntity)
                if (entry.Entity is BaseEntity baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        baseEntity.DateCreation = now;
                        baseEntity.CreePar = GetCurrentUserName();
                    }
                    if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                    {
                        baseEntity.DateModification = now;
                        baseEntity.ModifiePar = GetCurrentUserName();
                    }
                }

                context.Set<AuditLog>().Add(audit);
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                           ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

            if (Guid.TryParse(userIdClaim, out var guid))
            {
                return guid;
            }
            // Default System Admin ID if not found
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System";
        }
    }
}
