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
    }
}