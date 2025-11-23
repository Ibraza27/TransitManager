using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;

namespace TransitManager.Web.Services
{
    public interface IApiService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest);
        Task LogoutAsync();
        
        Task<IEnumerable<Client>?> GetClientsAsync();
        Task<UserProfileDto?> GetUserProfileAsync();
        Task<bool> UpdateUserProfileAsync(UserProfileDto profile);
        
        // --- Gestion des Colis ---
        Task<IEnumerable<ColisListItemDto>?> GetMyColisAsync();
        Task<Colis?> GetColisByIdAsync(Guid id);
        Task<bool> CreateColisAsync(CreateColisDto dto);
        Task<bool> UpdateColisAsync(Guid id, UpdateColisDto dto);
        Task<string?> GenerateBarcodeAsync();

        // --- Gestion des Conteneurs ---
        Task<IEnumerable<Conteneur>?> GetConteneursAsync();

        // --- Gestion des Paiements ---
        Task<IEnumerable<Paiement>?> GetPaiementsForColisAsync(Guid colisId);
        Task<Paiement?> CreatePaiementAsync(Paiement paiement);
        Task<bool> DeletePaiementAsync(Guid id);
		Task<bool> UpdateInventaireAsync(UpdateInventaireDto dto);
		Task<bool> DeleteColisAsync(Guid id); // <--- AJOUTER
		Task<bool> UpdatePaiementAsync(Guid id, Paiement paiement); // <--- AJOUTER
		Task<IEnumerable<VehiculeListItemDto>?> GetVehiculesAsync();
		
		Task<Client> GetClientByIdAsync(Guid id);
		Task<Vehicule> GetVehiculeByIdAsync(Guid id);
		Task<bool> CreateVehiculeAsync(Vehicule vehicule);
		Task<bool> UpdateVehiculeAsync(Guid id, Vehicule vehicule);
		Task<IEnumerable<Paiement>?> GetPaiementsForVehiculeAsync(Guid vehiculeId);
		Task<bool> DeleteVehiculeAsync(Guid id);
		Task<IEnumerable<Conteneur>?> GetMyConteneursAsync();
		Task<bool> DeleteConteneurAsync(Guid id);
		
    }
}