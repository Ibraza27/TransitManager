using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TransitManager.Web.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiService(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            };
        }

        private async Task PrepareAuthenticatedRequestAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                Console.WriteLine($"[ApiService] PrepareAuthenticatedRequestAsync - Token présent: {!string.IsNullOrEmpty(token)}");
                
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    Console.WriteLine($"[ApiService] PrepareAuthenticatedRequestAsync - Header Auth défini");
                }
                else
                {
                    Console.WriteLine("[ApiService] PrepareAuthenticatedRequestAsync - Aucun token trouvé");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] PrepareAuthenticatedRequestAsync - Erreur: {ex.Message}");
            }
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            try
            {
                Console.WriteLine($"[ApiService] Envoi login: {loginRequest.Email}");
                
                var response = await _httpClient.PostAsJsonAsync("api/auth/login-with-cookie", loginRequest);
                
                Console.WriteLine($"[ApiService] Réponse: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions);
                    // CORRECTION : Vérification de null sécurisée
                    Console.WriteLine($"[ApiService] Succès: {result?.Success}, Token: {!string.IsNullOrEmpty(result?.Token)}");
                    return result;
                }
                else
                {
                    // CORRECTION : Vérification de null pour response.Content
                    var errorContent = response.Content != null ? await response.Content.ReadAsStringAsync() : "Contenu d'erreur non disponible";
                    Console.WriteLine($"[ApiService] Erreur HTTP {response.StatusCode}: {errorContent}");
                }
                
                return new LoginResponseDto { Success = false, Message = $"Erreur (Code: {response.StatusCode})." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Exception: {ex.Message}");
                return new LoginResponseDto { Success = false, Message = $"Erreur réseau: {ex.Message}" };
            }
        }

        public async Task<IEnumerable<Client>?> GetClientsAsync()
        {
            try
            {
                Console.WriteLine("[ApiService] GetClientsAsync - Préparation de la requête authentifiée");
                await PrepareAuthenticatedRequestAsync();

                Console.WriteLine($"[ApiService] GetClientsAsync - Envoi requête GET à api/clients");
                Console.WriteLine($"[ApiService] GetClientsAsync - Header Auth: {_httpClient.DefaultRequestHeaders.Authorization}");

                var response = await _httpClient.GetAsync("api/clients");
                
                Console.WriteLine($"[ApiService] GetClientsAsync - Réponse: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var clients = await response.Content.ReadFromJsonAsync<IEnumerable<Client>>(_jsonOptions);
                    Console.WriteLine($"[ApiService] GetClientsAsync - Succès: {clients?.Count() ?? 0} clients");
                    return clients;
                }
                else
                {
                    // CORRECTION : Vérification de null pour response.Content
                    var errorContent = response.Content != null ? await response.Content.ReadAsStringAsync() : "Contenu d'erreur non disponible";
                    Console.WriteLine($"[ApiService] GetClientsAsync - Erreur {response.StatusCode}: {errorContent}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("[ApiService] GetClientsAsync - Erreur 401, token peut-être expiré");
                    }
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetClientsAsync - Exception: {ex.Message}");
                Console.WriteLine($"[ApiService] GetClientsAsync - StackTrace: {ex.StackTrace}");
                return null;
            }
        }
    }
}