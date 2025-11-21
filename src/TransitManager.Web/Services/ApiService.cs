using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

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
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            };
        }

        private async Task PrepareAuthenticatedRequestAsync()
        {
            Console.WriteLine("[ApiService] PrepareAuthenticatedRequestAsync n'est plus nécessaire, le cookie est géré par CookieHandler.");
            return;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            try
            {
                Console.WriteLine($"[ApiService] Envoi login: {loginRequest.Email}");
                var response = await _httpClient.PostAsJsonAsync("api/auth/login-with-cookie", loginRequest);
                Console.WriteLine($"[ApiService] Réponse de l'API: {response.StatusCode}");
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions);
                    Console.WriteLine($"[ApiService] Succès: {result?.Success}, Token: {!string.IsNullOrEmpty(result?.Token)}");
                    return result;
                }
                else
                {
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
                Console.WriteLine("[ApiService] GetClientsAsync - Envoi requête GET à api/clients (avec cookie via handler)");
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

        public async Task LogoutAsync()
        {
            try
            {
                Console.WriteLine("[ApiService] Envoi de la requête de déconnexion...");
                await _httpClient.PostAsync("api/auth/logout", null);
                Console.WriteLine("[ApiService] Requête de déconnexion terminée.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Erreur lors de la déconnexion : {ex.Message}");
            }
        }
		
		// Ajouter ces deux méthodes dans la classe ApiService

		public async Task<UserProfileDto?> GetUserProfileAsync()
		{
			try
			{
				// Le CookieHandler s'occupe d'envoyer l'auth
				return await _httpClient.GetFromJsonAsync<UserProfileDto>("api/profile");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ApiService] Erreur GetUserProfile: {ex.Message}");
				return null;
			}
		}

		public async Task<bool> UpdateUserProfileAsync(UserProfileDto profile)
		{
			try
			{
				var response = await _httpClient.PutAsJsonAsync("api/profile", profile);
				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[ApiService] Erreur UpdateUserProfile: {error}");
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ApiService] Exception UpdateUserProfile: {ex.Message}");
				return false;
			}
		}
		

		public async Task<IEnumerable<ColisListItemDto>?> GetMyColisAsync()
		{
			try
			{
				// CORRECTION : Ajout de _jsonOptions comme deuxième paramètre
				// Cela permet de comprendre le format "$id / $values" envoyé par l'API
				return await _httpClient.GetFromJsonAsync<IEnumerable<ColisListItemDto>>("api/colis/mine", _jsonOptions);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ApiService] Erreur GetMyColis: {ex.Message}");
				return null;
			}
		}
		

        public async Task<Colis?> GetColisByIdAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Colis>($"api/colis/{id}", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Erreur GetColisByIdAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> CreateColisAsync(CreateColisDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/colis", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Erreur CreateColisAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateColisAsync(Guid id, UpdateColisDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/colis/{id}", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Erreur UpdateColisAsync: {ex.Message}");
                return false;
            }
        }

        public async Task<string?> GenerateBarcodeAsync()
        {
            try
            {
                return await _httpClient.GetStringAsync("api/utilities/generate-barcode");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Erreur GenerateBarcodeAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<IEnumerable<Conteneur>?> GetConteneursAsync()
        {
            try
            {
                // Pour les listes simples, on peut utiliser l'option par défaut ou _jsonOptions selon l'API
                return await _httpClient.GetFromJsonAsync<IEnumerable<Conteneur>>("api/conteneurs", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Erreur GetConteneursAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<IEnumerable<Paiement>?> GetPaiementsForColisAsync(Guid colisId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Paiement>>($"api/paiements/colis/{colisId}", _jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Erreur GetPaiementsForColisAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<Paiement?> CreatePaiementAsync(Paiement paiement)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/paiements", paiement);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Paiement>(_jsonOptions);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Erreur CreatePaiementAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeletePaiementAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/paiements/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Erreur DeletePaiementAsync: {ex.Message}");
                return false;
            }
        }
		
    }
}
