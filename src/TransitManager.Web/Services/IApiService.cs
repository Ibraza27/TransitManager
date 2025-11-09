using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;

namespace TransitManager.Web.Services
{
    public interface IApiService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest);
        Task<IEnumerable<Client>?> GetClientsAsync();
        // Ajoutez d'autres méthodes au besoin
    }
}