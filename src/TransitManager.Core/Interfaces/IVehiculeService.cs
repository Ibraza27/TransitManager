using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;

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
    }
}