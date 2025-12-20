using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.DTOs; // AJOUT

namespace TransitManager.Core.Interfaces
{
    public interface IClientService
    {
		event Action<Guid> ClientStatisticsUpdated;
		
        Task<Client?> GetByIdAsync(Guid id);
        Task<Client?> GetByCodeAsync(string code);
        Task<IEnumerable<Client>> GetAllAsync();
        Task<IEnumerable<Client>> GetActiveClientsAsync();
        Task<IEnumerable<Client>> SearchAsync(string searchTerm);
        Task<Client> CreateAsync(Client client);
        Task<Client> UpdateAsync(Client client);
        Task<bool> DeleteAsync(Guid id);
        Task<int> GetTotalCountAsync();
        Task<int> GetNewClientsCountAsync(DateTime since);
        Task<IEnumerable<Client>> GetNewClientsListAsync(DateTime since);
        Task<IEnumerable<Client>> GetRecentClientsAsync(int count);
        Task<IEnumerable<Client>> GetClientsWithUnpaidBalanceAsync();
        Task<decimal> GetTotalUnpaidBalanceAsync();
        Task<bool> ExistsAsync(string email, string telephone, Guid? excludeId = null);
        Task<IEnumerable<Client>> GetClientsByConteneurAsync(Guid conteneurId);
		Task RecalculateAndUpdateClientStatisticsAsync(Guid clientId);
        Task<Dictionary<string, int>> GetNewClientsPerMonthAsync(int months);
        Task<Core.DTOs.PagedResult<Client>> GetPagedAsync(int page, int pageSize, string? search = null);
    }
}