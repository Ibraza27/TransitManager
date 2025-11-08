using System.Net.Http;
using System.Net.Http.Headers; // <-- AJOUTER CE USING
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
        private readonly ILocalStorageService _localStorage; // <-- AJOUTER CE CHAMP

        public ApiService(HttpClient httpClient, ILocalStorageService localStorage) // <-- MODIFIER LE CONSTRUCTEUR
        {
            _httpClient = httpClient;
            _localStorage = localStorage; // <-- AJOUTER CETTE LIGNE
            _jsonOptions = new JsonSerializerOptions
            {
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve,
                PropertyNameCaseInsensitive = true
            };
        }

        // La méthode LoginAsync n'a pas besoin de token, elle reste inchangée.
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
                // On récupère le token AVANT chaque requête
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                return await _httpClient.GetFromJsonAsync<IEnumerable<Client>>("api/clients", _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erreur API: {ex.StatusCode} - {ex.Message}");
                return null;
            }
        }
    }
}