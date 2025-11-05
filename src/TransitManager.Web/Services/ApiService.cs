using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;

namespace TransitManager.Web.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);
            
            // --- DÉBUT DE LA MODIFICATION ---
            // On essaie de lire le corps de la réponse QUEL QUE SOIT le code de statut,
            // tant qu'il y a un contenu.
            if (response.Content != null && response.Content.Headers.ContentLength > 0)
            {
                 return await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions);
            }
            // --- FIN DE LA MODIFICATION ---
            
            // Si la réponse est vide pour une raison quelconque, on renvoie une erreur générique.
            return new LoginResponseDto { Success = false, Message = $"Erreur du serveur (Code: {response.StatusCode})." };
        }
		
        public async Task<IEnumerable<Client>?> GetClientsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Client>>("api/clients", _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                // Gérer les erreurs de réseau ou les statuts 4xx/5xx
                Console.WriteLine($"Erreur API: {ex.StatusCode} - {ex.Message}");
                return null;
            }
        }
    }
}