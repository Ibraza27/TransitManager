using Microsoft.AspNetCore.Components.Forms;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using static TransitManager.Web.Services.ApiService;

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
		
		Task<bool> AssignColisToConteneurAsync(Guid colisId, Guid conteneurId);
		Task<bool> RemoveColisFromConteneurAsync(Guid colisId);
		Task<bool> AssignVehiculeToConteneurAsync(Guid vehiculeId, Guid conteneurId);
		Task<bool> RemoveVehiculeFromConteneurAsync(Guid vehiculeId);
		
		Task<ConteneurDetailDto?> GetConteneurDetailAsync(Guid id);
		Task<bool> AssignColisToConteneurListAsync(Guid conteneurId, List<Guid> colisIds);
		Task<bool> UnassignColisFromConteneurListAsync(Guid conteneurId, List<Guid> colisIds);
		Task<bool> AssignVehiculesToConteneurListAsync(Guid conteneurId, List<Guid> vehiculeIds);
		Task<bool> UnassignVehiculesFromConteneurListAsync(Guid conteneurId, List<Guid> vehiculeIds);
		
		Task<Conteneur?> CreateConteneurAsync(Conteneur conteneur);
		Task<bool> UpdateConteneurAsync(Guid id, Conteneur conteneur);
		
		Task<bool> UpdateClientAsync(Guid id, Client client);
		Task<bool> DeleteClientAsync(Guid id);
		Task<Client?> CreateClientAsync(Client client);
		
		Task<IEnumerable<Utilisateur>?> GetUsersAsync();
		Task<Utilisateur?> GetUserByIdAsync(Guid id);
		Task<bool> CreateUserAsync(Utilisateur user, string password);
		Task<bool> UpdateUserAsync(Guid id, Utilisateur user);
		Task<bool> DeleteUserAsync(Guid id);
		Task<string?> ResetPasswordAsync(Guid userId);
		Task<bool> UnlockUserAccountAsync(Guid id);
		Task<bool> ChangeUserPasswordAsync(Guid id, string newPassword);
		Task<PortalAccessResult> CreateOrResetPortalAccessAsync(Guid clientId);
		Task<bool> RegisterClientAsync(RegisterClientRequestDto request);
		Task<byte[]> ExportConteneurPdfAsync(Guid id, bool includeFinancials);

		// Ajoutez ces signatures dans l'interface IApiService
		Task<IEnumerable<Document>> GetDocumentsByEntityAsync(string entityType, Guid entityId);
		Task<Document?> UploadDocumentAsync(IBrowserFile file, TypeDocument type, Guid? clientId, Guid? vehiculeId, Guid? colisId, Guid? conteneurId);
		Task<byte[]> DownloadDocumentAsync(Guid id); // Retourne les bytes pour le téléchargement JS
		Task<bool> DeleteDocumentAsync(Guid id);
		
		Task<byte[]> ExportVehiculePdfAsync(Guid id, bool includeFinancials, bool includePhotos);
		Task<byte[]> ExportColisPdfAsync(Guid id, bool includeFinancials, bool includePhotos);
		Task<byte[]> ExportAttestationValeurPdfAsync(Guid id);
        Task<bool> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordDto request);
        Task<bool> VerifyEmailAsync(VerifyEmailDto request);
				
    }
}