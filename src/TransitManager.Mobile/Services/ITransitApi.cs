using Refit;
using TransitManager.Core.Entities;
using TransitManager.Core.DTOs;

namespace TransitManager.Mobile.Services
{
    public interface ITransitApi
    {
        [Get("/api/clients")]
        Task<IEnumerable<ClientListItemDto>> GetClientsAsync();

        [Get("/api/clients/{id}")]
        Task<Client> GetClientByIdAsync(Guid id);
        
        // --- AJOUTS ---
        [Post("/api/clients")]
        Task<Client> CreateClientAsync([Body] Client client);

        [Put("/api/clients/{id}")]
        Task UpdateClientAsync(Guid id, [Body] Client client);

        [Delete("/api/clients/{id}")]
        Task DeleteClientAsync(Guid id);

		
		// --- Colis ---
		[Get("/api/colis")]
		Task<IEnumerable<ColisListItemDto>> GetColisAsync();

		// --- AJOUTS ---
		[Get("/api/colis/{id}")]
		Task<Colis> GetColisByIdAsync(Guid id);

		[Post("/api/colis")]
		Task<Colis> CreateColisAsync([Body] Colis colis);

		[Put("/api/colis/{id}")]
		Task UpdateColisAsync(Guid id, [Body] Colis colis);

		[Delete("/api/colis/{id}")]
		Task DeleteColisAsync(Guid id);
		
		[Get("/api/vehicules")]
		Task<IEnumerable<VehiculeListItemDto>> GetVehiculesAsync();
		
		[Get("/api/vehicules/{id}")]
		Task<Vehicule> GetVehiculeByIdAsync(Guid id);
		
		[Post("/api/vehicules")]
		Task<Vehicule> CreateVehiculeAsync([Body] Vehicule vehicule);

		[Put("/api/vehicules/{id}")]
		Task UpdateVehiculeAsync(Guid id, [Body] Vehicule vehicule);
		
		[Get("/api/paiements/vehicule/{vehiculeId}")]
		Task<IEnumerable<Paiement>> GetPaiementsForVehiculeAsync(Guid vehiculeId);

		[Post("/api/paiements")]
		Task<Paiement> CreatePaiementAsync([Body] Paiement paiement);
		
		[Put("/api/paiements/{id}")]
		Task UpdatePaiementAsync(Guid id, [Body] Paiement paiement);

		[Delete("/api/paiements/{id}")]
		Task DeletePaiementAsync(Guid id);
    }
}