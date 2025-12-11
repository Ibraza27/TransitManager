using Refit;
using TransitManager.Core.Entities;
using TransitManager.Core.DTOs;

namespace TransitManager.Mobile.Services
{
    public interface ITransitApi
    {
        [Get("/api/clients")]
        Task<IEnumerable<Client>> GetClientsAsync(); 

        [Get("/api/clients/{id}")]
        Task<Client> GetClientByIdAsync(Guid id);
        
        [Post("/api/clients")]
        Task<Client> CreateClientAsync([Body] Client client);

        [Put("/api/clients/{id}")]
        Task UpdateClientAsync(Guid id, [Body] Client client);

        [Delete("/api/clients/{id}")]
        Task DeleteClientAsync(Guid id);

		
		// --- Colis ---
		[Get("/api/colis")]
		Task<IEnumerable<ColisListItemDto>> GetColisAsync();

		[Get("/api/colis/{id}")]
		Task<Colis> GetColisByIdAsync(Guid id);

		[Post("/api/colis")]
		Task<Colis> CreateColisAsync([Body] CreateColisDto colisDto);

		[Put("/api/colis/{id}")]
		Task UpdateColisAsync(Guid id, [Body] UpdateColisDto colisDto);
		
        [Put("/api/colis/inventaire")]
        Task UpdateInventaireAsync([Body] UpdateInventaireDto inventaireDto);

		[Delete("/api/colis/{id}")]
		Task DeleteColisAsync(Guid id);
		
		[Get("/api/vehicules")]
		Task<IEnumerable<VehiculeListItemDto>> GetVehiculesAsync();
		
		[Get("/api/conteneurs")]
		Task<IEnumerable<Conteneur>> GetConteneursAsync();
		
        // --- DÉBUT DE LA CORRECTION ---
        [Get("/api/conteneurs/{id}")]
        Task<Conteneur> GetConteneurByIdAsync(Guid id);
        // --- FIN DE LA CORRECTION ---
        
        [Post("/api/conteneurs")]
        Task<Conteneur> CreateConteneurAsync([Body] Conteneur conteneur);

        [Put("/api/conteneurs/{id}")]
        Task UpdateConteneurAsync(Guid id, [Body] Conteneur conteneur);
		
        [Delete("/api/conteneurs/{id}")]
        Task DeleteConteneurAsync(Guid id);
		
		[Get("/api/vehicules/{id}")]
		Task<Vehicule> GetVehiculeByIdAsync(Guid id);
		
		[Post("/api/vehicules")]
		Task<Vehicule> CreateVehiculeAsync([Body] Vehicule vehicule);

		[Put("/api/vehicules/{id}")]
		Task UpdateVehiculeAsync(Guid id, [Body] Vehicule vehicule);
		
		[Get("/api/paiements/vehicule/{vehiculeId}")]
		Task<IEnumerable<Paiement>> GetPaiementsForVehiculeAsync(Guid vehiculeId);

        [Get("/api/paiements/colis/{colisId}")]
        Task<IEnumerable<Paiement>> GetPaiementsForColisAsync(Guid colisId);

		[Post("/api/paiements")]
		Task<Paiement> CreatePaiementAsync([Body] Paiement paiement);
		
		[Put("/api/paiements/{id}")]
		Task UpdatePaiementAsync(Guid id, [Body] Paiement paiement);

		[Delete("/api/paiements/{id}")]
		Task DeletePaiementAsync(Guid id);
		
		[Get("/api/utilities/generate-barcode")]
		Task<string> GenerateBarcodeAsync();
		

        // --- Messagerie ---
        [Get("/api/messages")]
        Task<IEnumerable<MessageDto>> GetMessagesAsync([AliasAs("colisId")] Guid? colisId, [AliasAs("vehiculeId")] Guid? vehiculeId);

        [Post("/api/messages")]
        Task SendMessageAsync([Body] CreateMessageDto dto);

        [Post("/api/messages/mark-read")]
        Task MarkMessagesAsReadAsync([Body] object request); // On peut passer un objet anonyme

        // --- Timeline ---
        [Get("/api/timeline")]
        Task<IEnumerable<TimelineDto>> GetTimelineAsync([AliasAs("colisId")] Guid? colisId, [AliasAs("vehiculeId")] Guid? vehiculeId);
    }
}