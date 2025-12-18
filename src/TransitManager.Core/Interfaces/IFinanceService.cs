using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;

namespace TransitManager.Core.Interfaces
{
    public interface IFinanceService
    {
        // Admin
        Task<FinanceStatsDto> GetAdminStatsAsync(DateTime? startDate = null, DateTime? endDate = null, Guid? clientId = null);
        Task<IEnumerable<FinancialTransactionDto>> GetAllTransactionsAsync(DateTime? startDate, DateTime? endDate, Guid? clientId);
        
        // Client
        Task<ClientFinanceSummaryDto> GetClientSummaryAsync(Guid clientId);
        Task<IEnumerable<FinancialTransactionDto>> GetClientTransactionsAsync(Guid clientId);
    }
}
