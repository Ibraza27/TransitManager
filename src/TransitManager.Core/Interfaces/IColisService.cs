using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.DTOs;

namespace TransitManager.Core.Interfaces
{
    public interface IColisService
    {
        Task<Colis?> GetByIdAsync(Guid id);
        Task<Colis?> GetByBarcodeAsync(string barcode);
        Task<Colis?> GetByReferenceAsync(string reference);
        Task<IEnumerable<Colis>> GetAllAsync();
        Task<IEnumerable<Colis>> GetByClientAsync(Guid clientId);
        Task<IEnumerable<Colis>> GetByConteneurAsync(Guid conteneurId);
        Task<IEnumerable<Colis>> GetByStatusAsync(StatutColis statut);
        Task<Colis> CreateAsync(CreateColisDto colisDto);
        Task<Colis> UpdateAsync(Guid id, UpdateColisDto colisDto);
        Task<bool> DeleteAsync(Guid id);
        Task<Colis> ScanAsync(string barcode, string location);
        Task<bool> AssignToConteneurAsync(Guid colisId, Guid conteneurId);
        Task<bool> RemoveFromConteneurAsync(Guid colisId);
        Task<int> GetCountByStatusAsync(StatutColis statut);
        Task<IEnumerable<Colis>> GetRecentColisAsync(int count);
        Task<IEnumerable<Colis>> GetColisWaitingLongTimeAsync(int days);
        Task<bool> MarkAsDeliveredAsync(Guid colisId, string signature);
        Task<IEnumerable<Colis>> SearchAsync(string searchTerm);
        Task<Dictionary<StatutColis, int>> GetStatisticsByStatusAsync();
		Task<IEnumerable<Colis>> GetByUserIdAsync(Guid userId);

        // --- DÉBUT DE L'AJOUT ---
        Task RecalculateAndUpdateColisStatisticsAsync(Guid colisId);
		Task UpdateInventaireAsync(UpdateInventaireDto dto);
        // --- FIN DE L'AJOUT ---
		Task<PagedResult<ColisListItemDto>> GetPagedAsync(int page, int pageSize, string? search = null, Guid? clientId = null);
        Task<Dictionary<string, decimal>> GetMonthlyVolumeAsync(int months);
        Task<IEnumerable<Colis>> GetUnpricedColisAsync();
    }
}