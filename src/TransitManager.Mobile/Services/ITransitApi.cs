using Refit;
using TransitManager.Core.Entities;

namespace TransitManager.Mobile.Services
{
    public interface ITransitApi
    {
        [Get("/api/clients")]
        Task<IEnumerable<Client>> GetClientsAsync();

        [Get("/api/clients/{id}")]
        Task<Client> GetClientByIdAsync(Guid id);
        
        // Nous ajouterons les autres appels ici plus tard
    }
}