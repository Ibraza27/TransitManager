using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitManager.Web.Services
{
    public interface IApiService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest);
        Task<IEnumerable<Client>?> GetClientsAsync(); // <-- Cette méthode reste, mais sera appelée différemment
    }
}