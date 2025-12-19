using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using TransitManager.Core.Enums;
using Microsoft.AspNetCore.Components.Forms;

using Microsoft.JSInterop;

namespace TransitManager.Web.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IJSRuntime _jsRuntime;

        public ApiService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
				
            };
			_jsonOptions.Converters.Add(new JsonStringEnumConverter());
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

        public async Task<IEnumerable<Client>?> SearchClientsAsync(string term)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Client>>($"api/clients/search?term={Uri.EscapeDataString(term)}", _jsonOptions);
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

        public async Task<IEnumerable<SelectionItemDto>> GetAllEntitiesAsync(string type)
        {
            try
            {
                // We'll use a new endpoint or map existing ones.
                // For simplicity, let's call specific existing endpoints and map them, 
                // but preferably we should have a unified search endpoint.
                // Assuming we don't want to create new backend endpoints right now if possible:
                
                if (type == "Colis")
                {
                   // Create a new endpoint in Backend for this or use existing? 
                   // Existing `api/colis` returns `IEnumerable<Colis>`.
                   var items = await _httpClient.GetFromJsonAsync<IEnumerable<Colis>>("api/colis", _jsonOptions);
                   return items?.Select(c => new SelectionItemDto 
                   { 
                       Id = c.Id, 
                       Reference = c.NumeroReference, 
                       Description = $"{c.NombrePieces} colis - {c.Designation}", 
                       Info = c.Statut.ToString() 
                   }) ?? Enumerable.Empty<SelectionItemDto>();
                }
                else if (type == "Vehicule")
                {
                   var items = await _httpClient.GetFromJsonAsync<IEnumerable<Vehicule>>("api/vehicules", _jsonOptions);
                   return items?.Select(v => new SelectionItemDto 
                   { 
                       Id = v.Id, 
                       Reference = $"{v.Marque} {v.Modele}", 
                       Description = $"Immat: {v.Immatriculation}", 
                       Info = v.Statut.ToString() 
                   }) ?? Enumerable.Empty<SelectionItemDto>();
                }
                 else if (type == "Conteneur")
                {
                   var items = await _httpClient.GetFromJsonAsync<IEnumerable<Conteneur>>("api/conteneurs", _jsonOptions);
                   return items?.Select(c => new SelectionItemDto 
                   { 
                       Id = c.Id, 
                       Reference = c.NumeroDossier, 
                       Description = string.IsNullOrEmpty(c.NomCompagnie) ? "Conteneur" : c.NomCompagnie, 
                       Info = c.Statut.ToString() 
                   }) ?? Enumerable.Empty<SelectionItemDto>();
                }
                
                return Enumerable.Empty<SelectionItemDto>();
            }
            catch { return Enumerable.Empty<SelectionItemDto>(); }
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

        public async Task<Document?> RequestDocumentAsync(DocumentRequestDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/documents/request", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Document>(_jsonOptions);
                }
                return null;
            }
            catch { return null; }
        }

        public async Task<Document?> GetFirstMissingDocumentAsync(Guid clientId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<Document>($"api/documents/missing/first?clientId={clientId}", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<int> GetMissingDocumentsCountAsync(Guid clientId)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<JsonElement>($"api/documents/missing/count?clientId={clientId}");
                if(result.TryGetProperty("count", out var c)) return c.GetInt32();
                return 0;
            }
            catch { return 0; }
        }

        public async Task<IEnumerable<Document>> GetMissingDocumentsAsync(Guid clientId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Document>>($"api/documents/missing/all?clientId={clientId}", _jsonOptions)
                       ?? Enumerable.Empty<Document>();
            }
            catch { return Enumerable.Empty<Document>(); }
        }

        public async Task<IEnumerable<Document>> GetAllMissingDocumentsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Document>>($"api/dashboard/admin/missing-documents", _jsonOptions)
                       ?? Enumerable.Empty<Document>();
            }
            catch { return Enumerable.Empty<Document>(); }
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
			var url = $"api/conteneurs/{id}/export/pdf?includeFinancials={includeFinancials}";
			Console.WriteLine($"Step 3: [WEB SERVICE] Préparation de la requête GET vers : {url}");

			try
			{
				var response = await _httpClient.GetAsync(url);
				Console.WriteLine($"Step 6: [WEB SERVICE] Réponse reçue. Status Code : {response.StatusCode}");

				if (!response.IsSuccessStatusCode)
				{
					var errorContent = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"Step 6b: [WEB SERVICE] ERREUR API : {errorContent}");
					return Array.Empty<byte>();
				}

				var bytes = await response.Content.ReadAsByteArrayAsync();
				Console.WriteLine($"Step 6c: [WEB SERVICE] Succès. Taille téléchargée : {bytes.Length}");
				return bytes;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Step ERROR [WEB SERVICE]: Exception HTTP : {ex.Message}");
				return Array.Empty<byte>();
			}
		}
		

		public async Task<byte[]> ExportColisPdfAsync(Guid id, bool includeFinancials, bool includePhotos)
		{
			// On passe les deux paramètres booléens dans l'URL
			var url = $"api/colis/{id}/export/pdf?includeFinancials={includeFinancials}&includePhotos={includePhotos}";
			try
			{
				var response = await _httpClient.GetAsync(url);
				if (!response.IsSuccessStatusCode) return Array.Empty<byte>();
				return await response.Content.ReadAsByteArrayAsync();
			}
			catch
			{
				return Array.Empty<byte>();
			}
		}
						
		// Ajoutez ces méthodes dans la classe ApiService

		public async Task<IEnumerable<Document>> GetDocumentsByEntityAsync(string entityType, Guid entityId)
		{
			try
			{
				return await _httpClient.GetFromJsonAsync<IEnumerable<Document>>($"api/documents/entity/{entityType}/{entityId}", _jsonOptions) 
					   ?? Enumerable.Empty<Document>();
			}
			catch { return Enumerable.Empty<Document>(); }
		}

		public async Task<Document?> UploadDocumentAsync(IBrowserFile file, TypeDocument type, Guid? clientId, Guid? vehiculeId, Guid? colisId, Guid? conteneurId)
		{
			try
			{
				using var content = new MultipartFormDataContent();
				
				// Configuration importante pour les gros fichiers (ici max 10 Mo)
				var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024));
				fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
				
				content.Add(fileContent, "file", file.Name);
				content.Add(new StringContent(type.ToString()), "typeDocStr");
				
				if (clientId.HasValue) content.Add(new StringContent(clientId.Value.ToString()), "clientId");
				if (vehiculeId.HasValue) content.Add(new StringContent(vehiculeId.Value.ToString()), "vehiculeId");
				if (colisId.HasValue) content.Add(new StringContent(colisId.Value.ToString()), "colisId");
				if (conteneurId.HasValue) content.Add(new StringContent(conteneurId.Value.ToString()), "conteneurId");

				var response = await _httpClient.PostAsync("api/documents/upload", content);
				
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadFromJsonAsync<Document>(_jsonOptions);
				}
				return null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Erreur Upload: {ex.Message}");
				return null;
			}
		}

		public async Task<byte[]> DownloadDocumentAsync(Guid id)
		{
			try
			{
				var response = await _httpClient.GetAsync($"api/documents/{id}/download");
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadAsByteArrayAsync();
				}
				return Array.Empty<byte>();
			}
			catch { return Array.Empty<byte>(); }
		}

		public async Task<bool> DeleteDocumentAsync(Guid id)
		{
			try
			{
				var response = await _httpClient.DeleteAsync($"api/documents/{id}");
				return response.IsSuccessStatusCode;
			}
			catch { return false; }
		}
		
		public async Task<byte[]> ExportVehiculePdfAsync(Guid id, bool includeFinancials, bool includePhotos)
		{
			var url = $"api/vehicules/{id}/export/pdf?includeFinancials={includeFinancials}&includePhotos={includePhotos}";
			try
			{
				var response = await _httpClient.GetAsync(url);
				if (!response.IsSuccessStatusCode) return Array.Empty<byte>();
				return await response.Content.ReadAsByteArrayAsync();
			}
			catch
			{
				return Array.Empty<byte>();
			}
		}
		
		public async Task<byte[]> ExportAttestationValeurPdfAsync(Guid id)
		{
			var url = $"api/vehicules/{id}/export/attestation";
			try
			{
				var response = await _httpClient.GetAsync(url);
				if (!response.IsSuccessStatusCode) return Array.Empty<byte>();
				return await response.Content.ReadAsByteArrayAsync();
			}
			catch
			{
				return Array.Empty<byte>();
			}
		}
		
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            try
            {
                // L'API attend une string simple [FromBody], on l'envoie en JSON
                var response = await _httpClient.PostAsJsonAsync("api/auth/forgot-password", email);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur ForgotPassword: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur ResetPassword: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> VerifyEmailAsync(VerifyEmailDto request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/verify-email", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur VerifyEmail: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> ToggleUserEmailConfirmationAsync(Guid userId, bool isConfirmed)
        {
            try
            {
                // Envoi d'un booléen simple en JSON
                var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}/email-confirmation", isConfirmed);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> ResendUserConfirmationEmailAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/users/{userId}/resend-confirmation", null);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
		

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid? colisId, Guid? vehiculeId, Guid? conteneurId)
        {
            try
            {
                // Construction de la query string
                var query = colisId.HasValue ? $"colisId={colisId}" 
                          : vehiculeId.HasValue ? $"vehiculeId={vehiculeId}"
                          : $"conteneurId={conteneurId}";

                return await _httpClient.GetFromJsonAsync<IEnumerable<MessageDto>>($"api/messages?{query}", _jsonOptions) 
                       ?? Enumerable.Empty<MessageDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur GetMessages: {ex.Message}");
                return Enumerable.Empty<MessageDto>();
            }
        }

        public async Task<bool> SendMessageAsync(CreateMessageDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/messages", dto);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

		public async Task MarkMessagesAsReadAsync(Guid? colisId, Guid? vehiculeId, Guid? conteneurId)
        {
            try
            {
                // On inclut bien le conteneurId dans la requête
                var request = new { ColisId = colisId, VehiculeId = vehiculeId, ConteneurId = conteneurId };
                await _httpClient.PostAsJsonAsync("api/messages/mark-read", request);
            }
            catch 
            { 
                // Ignorer les erreurs de marquage (fire and forget)
            }
        }

        public async Task<IEnumerable<TimelineDto>> GetTimelineAsync(Guid? colisId, Guid? vehiculeId)
        {
            try
            {
                var query = colisId.HasValue ? $"colisId={colisId}" : $"vehiculeId={vehiculeId}";
                return await _httpClient.GetFromJsonAsync<IEnumerable<TimelineDto>>($"api/timeline?{query}", _jsonOptions) 
                       ?? Enumerable.Empty<TimelineDto>();
            }
            catch
            {
                return Enumerable.Empty<TimelineDto>();
            }
        }
		
		public async Task<byte[]> ExportTicketPdfAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/colis/{id}/export/ticket");
                if (!response.IsSuccessStatusCode) return Array.Empty<byte>();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
		

        // --- Notifications Implementation ---
        public async Task<IEnumerable<Notification>> GetMyNotificationsAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Notification>>("api/notifications", _jsonOptions) 
                       ?? Enumerable.Empty<Notification>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur GetMyNotifications: {ex.Message}");
                return Enumerable.Empty<Notification>();
            }
        }

        public async Task<int> GetUnreadNotificationsCountAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<JsonElement>("api/notifications/count");
                if (result.TryGetProperty("count", out var countProp))
                {
                    return countProp.GetInt32();
                }
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task MarkNotificationAsReadAsync(Guid id)
        {
            try
            {
                await _httpClient.PostAsync($"api/notifications/{id}/read", null);
            }
            catch { /* Ignorer les erreurs réseau sur une action secondaire */ }
        }

        public async Task MarkAllNotificationsAsReadAsync()
        {
            try
            {
                await _httpClient.PostAsync("api/notifications/read-all", null);
            }
            catch { }
        }
		
		public async Task<bool> CheckEntityExistsAsync(string entityType, Guid id)
		{
			try
			{
				string endpoint = entityType.ToLower() switch
				{
					"colis" => $"api/colis/{id}",
					"vehicule" => $"api/vehicules/{id}",
					"conteneur" => $"api/conteneurs/{id}",
					"paiement" => $"api/paiements/{id}",
					// Pour les autres types ou si inconnu, on laisse passer par défaut
					_ => null 
				};

				if (endpoint == null) return true;

				// On fait une requête légère. Si l'API renvoie 404, l'entité est supprimée.
				var response = await _httpClient.GetAsync(endpoint);
				return response.IsSuccessStatusCode;
			}
			catch
			{
				return false;
			}
		}



        public async Task<decimal> GetClientBalanceAsync(Guid clientId)
        {
             try
             {
                 var str = await _httpClient.GetStringAsync($"api/paiements/client/{clientId}/balance");
                 // Handle simple JSON number or raw string
                 if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var bal)) 
                     return bal;
                 return 0;
             }
             catch { return 0; }
        }

        public async Task<AdminDashboardStatsDto?> GetAdminDashboardStatsAsync()
        {
             try
             {
                 return await _httpClient.GetFromJsonAsync<AdminDashboardStatsDto>("api/dashboard/admin", _jsonOptions);
             }
             catch { return null; }
        }

        // --- FINANCE MODULE IMPL ---


        public async Task<ClientFinanceSummaryDto?> GetClientFinanceSummaryAsync(Guid clientId)
        {
            var response = await _httpClient.GetAsync($"api/finance/summary/{clientId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ClientFinanceSummaryDto>(_jsonOptions);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error {response.StatusCode}: {error}");
            }
        }

        public async Task<FinanceStatsDto?> GetFinanceStatsAsync(DateTime? startDate = null, DateTime? endDate = null, Guid? clientId = null)
        {
            try
            {
                var query = new List<string>();
                if (startDate.HasValue) query.Add($"start={startDate.Value:yyyy-MM-dd}");
                if (endDate.HasValue) query.Add($"end={endDate.Value:yyyy-MM-dd}");
                if (clientId.HasValue) query.Add($"clientId={clientId.Value}");
                
                var queryString = query.Any() ? "?" + string.Join("&", query) : "";
                
                return await _httpClient.GetFromJsonAsync<FinanceStatsDto>($"api/finance/stats{queryString}", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<IEnumerable<FinancialTransactionDto>?> GetTransactionsAsync(DateTime? startDate = null, DateTime? endDate = null, Guid? clientId = null)
        {
             try
            {
                var query = new List<string>();
                if (startDate.HasValue) query.Add($"start={startDate.Value:yyyy-MM-dd}");
                if (endDate.HasValue) query.Add($"end={endDate.Value:yyyy-MM-dd}");
                if (clientId.HasValue) query.Add($"clientId={clientId.Value}");
                
                var queryString = query.Any() ? "?" + string.Join("&", query) : "";
                
                return await _httpClient.GetFromJsonAsync<IEnumerable<FinancialTransactionDto>>($"api/finance/transactions{queryString}", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<IEnumerable<FinancialTransactionDto>?> GetClientTransactionsAsync(Guid clientId)
        {
             try
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<FinancialTransactionDto>>($"api/finance/client-transactions/{clientId}", _jsonOptions);
            }
            catch { return null; }
        }


        public async Task<TransitManager.Core.Entities.Paiement?> CreatePaymentAsync(TransitManager.Core.Entities.Paiement paiement)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/finance/payment", paiement, _jsonOptions);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransitManager.Core.Entities.Paiement>(_jsonOptions);
                return null;
            }
            catch { return null; }
        }

        public async Task<bool> DownloadReceiptAsync(Guid paiementId, string fileName)
        {
             try
            {
                var response = await _httpClient.GetAsync($"api/finance/receipt/{paiementId}");
                if (!response.IsSuccessStatusCode) return false;
                
                var content = await response.Content.ReadAsStreamAsync();
                using var streamRef = new DotNetStreamReference(content);
                await _jsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
                return true;
            }
             catch { return false; }
        }

        public async Task<bool> ExportTransactionsAsync(DateTime? start, DateTime? end)
        {
            try
            {
                var query = $"api/finance/export?";
                if(start.HasValue) query += $"start={start:yyyy-MM-dd}&";
                if(end.HasValue) query += $"end={end:yyyy-MM-dd}&";

                var response = await _httpClient.GetAsync(query);
                if (!response.IsSuccessStatusCode) return false;

                var fileName = $"Finance_{DateTime.Now:yyyyMMdd}.xlsx";
                var content = await response.Content.ReadAsStreamAsync();
                using var streamRef = new DotNetStreamReference(content);
                await _jsRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
                return true;
            }
             catch { return false; }
        }

    }
}
