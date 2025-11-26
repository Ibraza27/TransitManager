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

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login-with-cookie", loginRequest);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return await response.Content.ReadFromJsonAsync<LoginResponseDto>(_jsonOptions);
                }
                return new LoginResponseDto { Success = false, Message = $"Erreur (Code: {response.StatusCode})." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Exception: {ex.Message}");
                return new LoginResponseDto { Success = false, Message = $"Erreur réseau: {ex.Message}" };
            }
        }

        public async Task LogoutAsync()
        {
            try { await _httpClient.PostAsync("api/auth/logout", null); }
            catch { }
        }

        public async Task<IEnumerable<Client>?> GetClientsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Client>>("api/clients", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<UserProfileDto?> GetUserProfileAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<UserProfileDto>("api/profile");
            }
            catch { return null; }
        }

        public async Task<bool> UpdateUserProfileAsync(UserProfileDto profile)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/profile", profile);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<IEnumerable<ColisListItemDto>?> GetMyColisAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<ColisListItemDto>>("api/colis/mine", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<Colis?> GetColisByIdAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Colis>($"api/colis/{id}", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<bool> CreateColisAsync(CreateColisDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/colis", dto);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateColisAsync(Guid id, UpdateColisDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/colis/{id}", dto);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<string?> GenerateBarcodeAsync()
        {
            try
            {
                return await _httpClient.GetStringAsync("api/utilities/generate-barcode");
            }
            catch { return null; }
        }

        public async Task<IEnumerable<Conteneur>?> GetConteneursAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Conteneur>>("api/conteneurs", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<IEnumerable<Paiement>?> GetPaiementsForColisAsync(Guid colisId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Paiement>>($"api/paiements/colis/{colisId}", _jsonOptions);
            }
            catch { return null; }
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
            catch { return null; }
        }

        public async Task<bool> DeletePaiementAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/paiements/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateInventaireAsync(UpdateInventaireDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/colis/inventaire", dto);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteColisAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/colis/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdatePaiementAsync(Guid id, Paiement paiement)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/paiements/{id}", paiement);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<IEnumerable<VehiculeListItemDto>?> GetVehiculesAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<VehiculeListItemDto>>("api/vehicules/mine", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<Vehicule> GetVehiculeByIdAsync(Guid id)
        {
            return await _httpClient.GetFromJsonAsync<Vehicule>($"api/vehicules/{id}", _jsonOptions);
        }

        public async Task<Client> GetClientByIdAsync(Guid id)
        {
            return await _httpClient.GetFromJsonAsync<Client>($"api/clients/{id}", _jsonOptions);
        }

        public async Task<bool> CreateVehiculeAsync(Vehicule vehicule)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/vehicules", vehicule, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateVehiculeAsync(Guid id, Vehicule vehicule)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/vehicules/{id}", vehicule, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<IEnumerable<Paiement>?> GetPaiementsForVehiculeAsync(Guid vehiculeId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Paiement>>($"api/paiements/vehicule/{vehiculeId}", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<bool> DeleteVehiculeAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/vehicules/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<IEnumerable<Conteneur>?> GetMyConteneursAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Conteneur>>("api/conteneurs/mine", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<bool> DeleteConteneurAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/conteneurs/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> AssignColisToConteneurAsync(Guid colisId, Guid conteneurId)
        {
            var colis = await GetColisByIdAsync(colisId);
            if (colis == null) return false;

            var dto = new UpdateColisDto
            {
                Id = colis.Id,
                ClientId = colis.ClientId,
                Designation = colis.Designation,
                DestinationFinale = colis.DestinationFinale,
                Barcodes = colis.Barcodes.Select(b => b.Value).ToList(),
                NombrePieces = colis.NombrePieces,
                Volume = colis.Volume,
                ValeurDeclaree = colis.ValeurDeclaree,
                PrixTotal = colis.PrixTotal,
                SommePayee = colis.SommePayee,
                Destinataire = colis.Destinataire,
                TelephoneDestinataire = colis.TelephoneDestinataire,
                LivraisonADomicile = colis.LivraisonADomicile,
                AdresseLivraison = colis.AdresseLivraison,
                EstFragile = colis.EstFragile,
                ManipulationSpeciale = colis.ManipulationSpeciale,
                InstructionsSpeciales = colis.InstructionsSpeciales,
                Type = colis.Type,
                TypeEnvoi = colis.TypeEnvoi,
                InventaireJson = colis.InventaireJson,
                ConteneurId = conteneurId,
                Statut = Core.Enums.StatutColis.Affecte
            };

            return await UpdateColisAsync(colisId, dto);
        }

        public async Task<bool> RemoveColisFromConteneurAsync(Guid colisId)
        {
            var colis = await GetColisByIdAsync(colisId);
            if (colis == null) return false;

            var dto = new UpdateColisDto
            {
                Id = colis.Id,
                ClientId = colis.ClientId,
                Designation = colis.Designation,
                DestinationFinale = colis.DestinationFinale,
                Barcodes = colis.Barcodes.Select(b => b.Value).ToList(),
                NombrePieces = colis.NombrePieces,
                Volume = colis.Volume,
                ValeurDeclaree = colis.ValeurDeclaree,
                PrixTotal = colis.PrixTotal,
                SommePayee = colis.SommePayee,
                Destinataire = colis.Destinataire,
                TelephoneDestinataire = colis.TelephoneDestinataire,
                LivraisonADomicile = colis.LivraisonADomicile,
                AdresseLivraison = colis.AdresseLivraison,
                EstFragile = colis.EstFragile,
                ManipulationSpeciale = colis.ManipulationSpeciale,
                InstructionsSpeciales = colis.InstructionsSpeciales,
                Type = colis.Type,
                TypeEnvoi = colis.TypeEnvoi,
                InventaireJson = colis.InventaireJson,
                ConteneurId = null,
                Statut = Core.Enums.StatutColis.EnAttente
            };

            return await UpdateColisAsync(colisId, dto);
        }

        public async Task<bool> AssignVehiculeToConteneurAsync(Guid vehiculeId, Guid conteneurId)
        {
            var vehicule = await GetVehiculeByIdAsync(vehiculeId);
            if (vehicule == null) return false;

            vehicule.ConteneurId = conteneurId;
            vehicule.Statut = Core.Enums.StatutVehicule.Affecte;
            vehicule.Client = null;
            vehicule.Conteneur = null;
            vehicule.Paiements = null;

            return await UpdateVehiculeAsync(vehiculeId, vehicule);
        }

        public async Task<bool> RemoveVehiculeFromConteneurAsync(Guid vehiculeId)
        {
            var vehicule = await GetVehiculeByIdAsync(vehiculeId);
            if (vehicule == null) return false;

            vehicule.ConteneurId = null;
            vehicule.Statut = Core.Enums.StatutVehicule.EnAttente;
            vehicule.Client = null;
            vehicule.Conteneur = null;
            vehicule.Paiements = null;

            return await UpdateVehiculeAsync(vehiculeId, vehicule);
        }

        public async Task<ConteneurDetailDto?> GetConteneurDetailAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ConteneurDetailDto>($"api/conteneurs/{id}/detail", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<bool> AssignColisToConteneurListAsync(Guid conteneurId, List<Guid> colisIds)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/conteneurs/{conteneurId}/colis/assign", colisIds);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnassignColisFromConteneurListAsync(Guid conteneurId, List<Guid> colisIds)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/conteneurs/{conteneurId}/colis/unassign", colisIds);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AssignVehiculesToConteneurListAsync(Guid conteneurId, List<Guid> vehiculeIds)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/conteneurs/{conteneurId}/vehicules/assign", vehiculeIds);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UnassignVehiculesFromConteneurListAsync(Guid conteneurId, List<Guid> vehiculeIds)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/conteneurs/{conteneurId}/vehicules/unassign", vehiculeIds);
            return response.IsSuccessStatusCode;
        }

        public async Task<Conteneur?> CreateConteneurAsync(Conteneur conteneur)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/conteneurs", conteneur, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Conteneur>(_jsonOptions);
                }
                return null;
            }
            catch { return null; }
        }

        public async Task<bool> UpdateConteneurAsync(Guid id, Conteneur conteneur)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/conteneurs/{id}", conteneur, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
		
		public async Task<Client?> CreateClientAsync(Client client)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/clients", client, _jsonOptions);
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadFromJsonAsync<Client>(_jsonOptions);
				}
				else
				{
					var error = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[ApiService] Erreur CreateClientAsync: {response.StatusCode} - {error}");
					return null;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ApiService] Exception CreateClientAsync: {ex.Message}");
				return null;
			}
		}

		public async Task<bool> UpdateClientAsync(Guid id, Client client)
		{
			try
			{
				var response = await _httpClient.PutAsJsonAsync($"api/clients/{id}", client, _jsonOptions);
				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[ApiService] Erreur UpdateClientAsync: {response.StatusCode} - {error}");
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ApiService] Exception UpdateClientAsync: {ex.Message}");
				return false;
			}
		}

		public async Task<bool> DeleteClientAsync(Guid id)
		{
			try
			{
				var response = await _httpClient.DeleteAsync($"api/clients/{id}");
				return response.IsSuccessStatusCode;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ApiService] Erreur DeleteClientAsync: {ex.Message}");
				return false;
			}
		}
		
		public async Task<IEnumerable<Utilisateur>?> GetUsersAsync()
		{
			try { return await _httpClient.GetFromJsonAsync<IEnumerable<Utilisateur>>("api/users", _jsonOptions); }
			catch { return null; }
		}

		public async Task<Utilisateur?> GetUserByIdAsync(Guid id)
		{
			try { return await _httpClient.GetFromJsonAsync<Utilisateur>($"api/users/{id}", _jsonOptions); }
			catch { return null; }
		}

		public async Task<bool> CreateUserAsync(Utilisateur user, string password)
		{
			try
			{
				// On doit envelopper l'utilisateur et le mot de passe comme attendu par l'API
				var request = new { User = user, Password = password };
				var response = await _httpClient.PostAsJsonAsync("api/users", request, _jsonOptions);
				return response.IsSuccessStatusCode;
			}
			catch { return false; }
		}

		public async Task<bool> UpdateUserAsync(Guid id, Utilisateur user)
		{
			try
			{
				var response = await _httpClient.PutAsJsonAsync($"api/users/{id}", user, _jsonOptions);
				return response.IsSuccessStatusCode;
			}
			catch { return false; }
		}

		public async Task<bool> DeleteUserAsync(Guid id)
		{
			try
			{
				var response = await _httpClient.DeleteAsync($"api/users/{id}");
				return response.IsSuccessStatusCode;
			}
			catch { return false; }
		}
		
		public async Task<string?> ResetPasswordAsync(Guid userId)
		{
			try
			{
				// CORRECTION : On utilise bien userId
				var response = await _httpClient.PostAsync($"api/users/{userId}/reset-password", null);
				
				if (response.IsSuccessStatusCode)
				{
					// On récupère le mot de passe en clair (probablement entouré de guillemets car c'est du JSON string)
					var raw = await response.Content.ReadAsStringAsync();
					return raw.Trim('"'); // On enlève les guillemets éventuels du JSON
				}
				return null;
			}
			catch { return null; }
		}

		public async Task<bool> UnlockUserAccountAsync(Guid id)
		{
			try
			{
				var response = await _httpClient.PostAsync($"api/users/{id}/unlock", null);
				return response.IsSuccessStatusCode;
			}
			catch { return false; }
		}

		public async Task<bool> ChangeUserPasswordAsync(Guid id, string newPassword)
		{
			try
			{
				var request = new { NewPassword = newPassword };
				var response = await _httpClient.PostAsJsonAsync($"api/users/{id}/change-password", request, _jsonOptions);
				return response.IsSuccessStatusCode;
			}
			catch { return false; }
		}		
		
		// Classe pour le résultat (à mettre dans le namespace ou à part)
		public class PortalAccessResult
		{
			public string Message { get; set; } = "";
			public Guid UserId { get; set; }
			public string Username { get; set; } = "";
			public string TemporaryPassword { get; set; } = "";
		}

		// Dans la classe ApiService
		public async Task<PortalAccessResult> CreateOrResetPortalAccessAsync(Guid clientId)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/users/create-portal-access", new { ClientId = clientId });
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadFromJsonAsync<PortalAccessResult>(_jsonOptions) ?? new();
				}
				// Gérer l'erreur si besoin
				return new PortalAccessResult { Message = "Erreur API" };
			}
			catch (Exception ex) { return new PortalAccessResult { Message = ex.Message }; }
		}
		
		public async Task<bool> RegisterClientAsync(RegisterClientRequestDto request)
		{
			try
			{
				var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"Register Error: {error}");
					// Idéalement, on devrait remonter le message d'erreur à la vue
					throw new Exception(error); // On lance une exception pour que la vue l'attrape
				}
				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				throw;
			}
		}
		
		public async Task<byte[]> ExportConteneurPdfAsync(Guid id, bool includeFinancials)
		{
			try
			{
				var url = $"api/conteneurs/{id}/export/pdf?includeFinancials={includeFinancials}";
				Console.WriteLine($"[ApiService] Tentative de téléchargement PDF : {url}");

				var response = await _httpClient.GetAsync(url);
				
				if (!response.IsSuccessStatusCode)
				{
					var error = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[ApiService] ❌ Erreur API ({response.StatusCode}) : {error}");
					return Array.Empty<byte>();
				}

				var bytes = await response.Content.ReadAsByteArrayAsync();
				Console.WriteLine($"[ApiService] ✅ PDF reçu : {bytes.Length} octets");
				return bytes;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ApiService] 💥 Exception : {ex.Message}");
				return Array.Empty<byte>();
			}
		}
		
    }
}