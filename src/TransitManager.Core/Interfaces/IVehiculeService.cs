using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.DTOs; // AJOUT

namespace TransitManager.Core.Interfaces
{
    public interface IVehiculeService
    {
        Task<Vehicule?> GetByIdAsync(Guid id);
        Task<IEnumerable<Vehicule>> GetAllAsync();
        Task<IEnumerable<Vehicule>> GetByClientAsync(Guid clientId);
        Task<Vehicule> CreateAsync(Vehicule vehicule);
        Task<Vehicule> UpdateAsync(Vehicule vehicule);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<Vehicule>> SearchAsync(string searchTerm);
		Task<bool> RemoveFromConteneurAsync(Guid vehiculeId);
		Task<bool> AssignToConteneurAsync(Guid vehiculeId, Guid conteneurId);
        Task RecalculateAndUpdateVehiculeStatisticsAsync(Guid vehiculeId);
		Task<IEnumerable<Vehicule>> GetByUserIdAsync(Guid userId);
        Task<Core.DTOs.PagedResult<Core.DTOs.VehiculeListItemDto>> GetPagedAsync(int page, int pageSize, string? search = null, Guid? clientId = null);
        Task<IEnumerable<Vehicule>> GetDelayedVehiculesAsync(int days);
        Task<IEnumerable<Vehicule>> GetUnpricedVehiculesAsync();
    }
}