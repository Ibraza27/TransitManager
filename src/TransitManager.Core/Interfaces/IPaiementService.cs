using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Interfaces
{
    public interface IPaiementService
    {
        Task<Paiement?> GetByIdAsync(Guid id);
        Task<IEnumerable<Paiement>> GetAllAsync();
        Task<IEnumerable<Paiement>> GetByClientAsync(Guid clientId);
        Task<IEnumerable<Paiement>> GetByConteneurAsync(Guid conteneurId);
		Task<IEnumerable<Paiement>> GetByColisAsync(Guid colisId);
		Task<IEnumerable<Paiement>> GetByVehiculeAsync(Guid vehiculeId);
        Task<IEnumerable<Paiement>> GetByPeriodAsync(DateTime debut, DateTime fin);
        Task<Paiement> CreateAsync(Paiement paiement);
        Task<Paiement> UpdateAsync(Paiement paiement);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ValidatePaymentAsync(Guid paiementId);
        Task<bool> CancelPaymentAsync(Guid paiementId, string raison);
        Task<decimal> GetMonthlyRevenueAsync(DateTime month);
        Task<decimal> GetPendingAmountAsync();
        Task<IEnumerable<Paiement>> GetOverduePaymentsAsync();
        Task<Dictionary<TypePaiement, decimal>> GetPaymentsByTypeAsync(DateTime debut, DateTime fin);
        Task<bool> SendPaymentReminderAsync(Guid clientId);
        Task<byte[]> GenerateReceiptAsync(Guid paiementId);
        Task<decimal> GetClientBalanceAsync(Guid clientId);
        Task<bool> RecordPartialPaymentAsync(Guid clientId, decimal montant, TypePaiement type, string? reference = null);
        Task<Dictionary<string, decimal>> GetMonthlyRevenueHistoryAsync(int months);
    }
}