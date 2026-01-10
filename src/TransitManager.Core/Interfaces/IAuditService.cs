using System;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;

namespace TransitManager.Core.Interfaces
{
    public interface IAuditService
    {
        Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(int page, int pageSize, string? userId = null, string? entityName = null, DateTime? date = null);
        Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id);
    }
}
