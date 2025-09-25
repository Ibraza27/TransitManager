using Refit;
using TransitManager.Core.Entities;
using TransitManager.Core.DTOs; // <-- AJOUTER CE USING

namespace TransitManager.Mobile.Services
{
    public interface ITransitApi
    {
        // MODIFICATION : Le type de retour est maintenant le DTO
        [Get("/api/clients")]
        Task<IEnumerable<ClientListItemDto>> GetClientsAsync();

        [Get("/api/clients/{id}")]
        Task<Client> GetClientByIdAsync(Guid id);
    }
}