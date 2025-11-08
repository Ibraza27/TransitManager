using TransitManager.Core.DTOs;
using TransitManager.Core.Entities; // <-- AJOUTER CE USING

namespace TransitManager.Web.Services
{
    public interface IApiService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest);
        
        // AJOUTER LA LIGNE CI-DESSOUS
        Task<IEnumerable<Client>?> GetClientsAsync();
    }
}