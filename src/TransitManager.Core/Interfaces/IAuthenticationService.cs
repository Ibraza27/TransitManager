using System.Threading.Tasks;
using TransitManager.Core.Enums;
using TransitManager.Core.Entities;

namespace TransitManager.Core.Interfaces
{
    public interface IAuthenticationService
    {
        Utilisateur? CurrentUser { get; }
        Task<AuthenticationResult> LoginAsync(string username, string password);
        Task LogoutAsync();
        Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
        // ... autres méthodes ...
    }


}