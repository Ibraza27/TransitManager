using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using System.Collections.Generic; // S'assurer que ce using est là

namespace TransitManager.Web.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        // --- RETOUR AU CONSTRUCTEUR SIMPLE ---
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

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions);
            }
            
            // Si le login échoue, on peut essayer de lire le message d'erreur
            if (response.Content != null && response.Content.Headers.ContentLength > 0)
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions);
                return errorResponse;
            }

            return new LoginResponseDto { Success = false, Message = $"Erreur (Code: {response.StatusCode})." };
        }

        // --- LA MÉTHODE REDEVIENT SIMPLE ---
        public async Task<IEnumerable<Client>?> GetClientsAsync()
        {
            try
            {
                // Plus besoin de gérer le token ici.
                return await _httpClient.GetFromJsonAsync<IEnumerable<Client>>("api/clients", _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erreur API GetClients: {ex.StatusCode} - {ex.Message}");
                return null;
            }
        }
    }
}