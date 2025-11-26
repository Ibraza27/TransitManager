// src/TransitManager.Core/Interfaces/IAuthenticationService.cs

using System; // Assurez-vous que ce using est là
using System.Threading.Tasks;
using TransitManager.Core.Enums;
using TransitManager.Core.Entities;
using TransitManager.Core.DTOs; // <-- AJOUTEZ CE USING

namespace TransitManager.Core.Interfaces
{
    public interface IAuthenticationService
    {
        Utilisateur? CurrentUser { get; }
        Task<AuthenticationResult> LoginAsync(string identifier, string password);
        Task LogoutAsync();
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

        // === AJOUTER CES DEUX MÉTHODES ===
        Task<(Utilisateur? User, string? TemporaryPassword)> CreateOrResetPortalAccessAsync(Guid clientId);
        Task SynchronizeClientDataAsync(Client client);
		Task<AuthenticationResult> RegisterClientAsync(RegisterClientRequestDto request);
    }
}