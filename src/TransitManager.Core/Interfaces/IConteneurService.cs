using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities; // <-- using important
using TransitManager.Core.Enums; // <-- using important

namespace TransitManager.Core.Interfaces
{
    public interface IConteneurService
    {
        Task<Conteneur?> GetByIdAsync(Guid id);
        Task<Conteneur?> GetByNumeroDossierAsync(string numeroDossier);
        Task<IEnumerable<Conteneur>> GetAllAsync();
        Task<IEnumerable<Conteneur>> GetActiveAsync();
        Task<IEnumerable<Conteneur>> GetByDestinationAsync(string destination);
        Task<IEnumerable<Conteneur>> GetByStatusAsync(StatutConteneur statut);
        Task<Conteneur> CreateAsync(Conteneur conteneur);
        Task<Conteneur> UpdateAsync(Conteneur conteneur);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> CloseConteneurAsync(Guid id);
        Task<bool> SetDepartureAsync(Guid id, DateTime departureDate);
        Task<bool> SetArrivalAsync(Guid id, DateTime arrivalDate);
        Task<int> GetActiveCountAsync();
        Task<decimal> GetAverageFillingRateAsync();
        Task<IEnumerable<Conteneur>> GetUpcomingDeparturesAsync(int days);
        Task<IEnumerable<Conteneur>> GetAlmostFullContainersAsync(decimal threshold);
        Task<Dictionary<string, int>> GetStatsByDestinationAsync();
        Task<bool> CanAddColisAsync(Guid conteneurId, Guid colisId);
        Task<decimal> CalculateProfitabilityAsync(Guid conteneurId);
		Task<IEnumerable<string>> GetAllDestinationsAsync();
		Task<IEnumerable<Conteneur>> GetOpenConteneursAsync();
		Task RecalculateStatusAsync(Guid conteneurId);
    }
}