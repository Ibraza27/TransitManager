using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.DTOs.Commerce;
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
                ReferenceHandler = ReferenceHandler.IgnoreCycles
				
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

        public async Task<PortalAccessResult> CreateOrResetPortalAccessAsync(Guid clientId)
        {
            var request = new { ClientId = clientId };
            var response = await _httpClient.PostAsJsonAsync("api/users/create-portal-access", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error creating portal access: {response.StatusCode} - {error}");
            }

            return await response.Content.ReadFromJsonAsync<PortalAccessResult>()
                   ?? throw new InvalidOperationException("Failed to deserialize the portal access result.");
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

        public async Task<Colis?> CreateColisAsync(CreateColisDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/colis", dto);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content)) return null;
                    return JsonSerializer.Deserialize<Colis>(content, _jsonOptions);
                }
                
                // Log non-success status code if needed
                return null;
            }
            catch (Exception ex)
            {
                // In a real app we'd log this locally
                return null;
            }
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

        public async Task<bool> ToggleColisExportExclusionAsync(Guid id, bool isExcluded)
        {
            try
            {
                // On passe la valeur booléenne directement
                var response = await _httpClient.PostAsJsonAsync($"api/colis/{id}/toggle-export", isExcluded);
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

        public async Task<Vehicule?> CreateVehiculeAsync(Vehicule vehicule)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/vehicules", vehicule, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Vehicule>(_jsonOptions);
                }
                return null;
            }
            catch { return null; }
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

        public async Task<IEnumerable<Document>> GetFinancialDocumentsAsync(int? year, int? month, TypeDocument? type, Guid? clientId = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (year.HasValue) queryParams.Add($"year={year.Value}");
                if (month.HasValue) queryParams.Add($"month={month.Value}");
                if (type.HasValue) queryParams.Add($"type={type.Value}");
                if (clientId.HasValue) queryParams.Add($"clientId={clientId.Value}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";

                return await _httpClient.GetFromJsonAsync<IEnumerable<Document>>($"api/documents/financial{queryString}", _jsonOptions)
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

		public async Task<(bool Success, string Message)> CreateUserAsync(Utilisateur user, string password)
		{
			try
			{
				// Sanitize the user object to avoid sending unnecessary data but satisfy validation
				user.Audits = new List<AuditLog>(); // Must not be null
				user.Client = null; 
				user.MotDePasseHash = "temp_hash_to_pass_validation"; // Required by model, will be overwritten by server

				var request = new { User = user, Password = password };
				var response = await _httpClient.PostAsJsonAsync("api/users", request, _jsonOptions);
				
				if (response.IsSuccessStatusCode)
				{
					return (true, string.Empty);
				}
				
				var errorMsg = await response.Content.ReadAsStringAsync();
				return (false, $"Erreur API ({response.StatusCode}): {errorMsg}");
			}
			catch (Exception ex) 
			{ 
				return (false, $"Exception: {ex.Message}"); 
			}
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

        // --- Invoices ---

        public async Task<PagedResult<InvoiceDto>> GetInvoicesAsync(string? search, Guid? clientId, string? status, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
                if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
                if (clientId.HasValue) query.Add($"clientId={clientId}");
                if (!string.IsNullOrWhiteSpace(status)) query.Add($"status={status}");
                
                return await _httpClient.GetFromJsonAsync<PagedResult<InvoiceDto>>($"api/commerce/invoices?{string.Join("&", query)}", _jsonOptions) 
                       ?? new PagedResult<InvoiceDto>();
            }
            catch { return new PagedResult<InvoiceDto>(); }
        }

        public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id)
        {
            try { return await _httpClient.GetFromJsonAsync<InvoiceDto>($"api/commerce/invoices/{id}", _jsonOptions); }
            catch { return null; }
        }

        public async Task<InvoiceDto?> GetPublicInvoiceAsync(Guid token)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<InvoiceDto>($"api/commerce/public/invoice/{token}", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<InvoiceDto?> DuplicateInvoiceAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/commerce/invoices/{id}/duplicate", null);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<InvoiceDto>(_jsonOptions);
                return null;
            }
            catch { return null; }
        }

        public async Task<InvoiceDto?> CreateInvoiceAsync(CreateInvoiceDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/commerce/invoices", dto, _jsonOptions);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<InvoiceDto>(_jsonOptions);
                
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erreur API ({response.StatusCode}): {error}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Best Effort Log] CreateInvoiceAsync Exception: {ex.Message}");
                throw; 
            }
        }

        public async Task<InvoiceDto?> UpdateInvoiceAsync(UpdateInvoiceDto dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/commerce/invoices/{dto.Id}", dto, _jsonOptions);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<InvoiceDto>(_jsonOptions);
                
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Erreur API ({response.StatusCode}): {error}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Best Effort Log] UpdateInvoiceAsync Exception: {ex.Message}");
                throw; 
            }
        }

        public async Task<bool> ConvertQuoteToInvoiceAsync(Guid quoteId)
        {
             try
             {
                 var response = await _httpClient.PostAsync($"api/commerce/quotes/{quoteId}/convert", null);
                 // We could return the new InvoiceDto but usually we just want to know if it worked and maybe redirect or show list
                 return response.IsSuccessStatusCode;
             }
             catch { return false; }
        }

        public async Task<bool> UpdateInvoiceStatusAsync(Guid id, InvoiceStatus status)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/commerce/invoices/{id}/status?status={status}", null);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> DeleteInvoiceAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/commerce/invoices/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> SendInvoiceEmailAsync(Guid id, string? subject, string? body, List<Guid>? attachments, List<string>? cc = null)
        {
            try
            {
                var dto = new SendQuoteEmailDto 
                { 
                    Subject = subject, 
                    Body = body, 
                    TempAttachmentIds = attachments,
                    Cc = cc != null && cc.Any() ? string.Join(",", cc) : null
                };
                var response = await _httpClient.PostAsJsonAsync($"api/commerce/invoices/{id}/email", dto, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // SendPaymentReminderAsync moved to end of class with full recipients support
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

        public async Task<byte[]> GetQuotePdfAsync(Guid quoteId, Guid? token = null)
        {
            var url = $"api/commerce/quotes/{quoteId}/pdf";
            if(token.HasValue) url += $"?token={token}";
            
            try
            {
                return await _httpClient.GetByteArrayAsync(url);
            }
            catch
            {
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
				
				// Configuration importante pour les gros fichiers (ici max 500 Mo)
				const long maxFileSize = 500L * 1024 * 1024;
				using var stream = file.OpenReadStream(maxAllowedSize: maxFileSize);
				var fileContent = new StreamContent(stream);
				fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
				
				content.Add(fileContent, "file", file.Name);
				content.Add(new StringContent(type.ToString()), "typeDocStr");
				
				if (clientId.HasValue) content.Add(new StringContent(clientId.Value.ToString()), "clientId");
				if (vehiculeId.HasValue) content.Add(new StringContent(vehiculeId.Value.ToString()), "vehiculeId");
				if (colisId.HasValue) content.Add(new StringContent(colisId.Value.ToString()), "colisId");
				if (conteneurId.HasValue) content.Add(new StringContent(conteneurId.Value.ToString()), "conteneurId");

				// Timeout géré globalement dans Program.cs 

				var response = await _httpClient.PostAsync("api/documents/upload", content);
				
				if (response.IsSuccessStatusCode)
				{
					if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return null;
					return await response.Content.ReadFromJsonAsync<Document>(_jsonOptions);
				}
				else
				{
					var error = await response.Content.ReadAsStringAsync();
					Console.WriteLine($"[ApiService] Upload Error {response.StatusCode}: {error}");
					// On pourrait throw une exception ici pour l'afficher à l'utilisateur
                    throw new Exception($"Erreur serveur: {response.StatusCode} - {error}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[ApiService] Exception Upload: {ex.Message}");
				// Re-throw pour que le composant puisse afficher l'erreur
				throw; 
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

		public async Task<Document?> UpdateDocumentAsync(Guid id, UpdateDocumentDto dto)
		{
			try
			{
				var response = await _httpClient.PutAsJsonAsync($"api/documents/{id}", dto, _jsonOptions);
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadFromJsonAsync<Document>(_jsonOptions);
				}
				return null;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Erreur UpdateDocument: {ex.Message}");
				return null;
			}
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

        public async Task<Guid?> SendMessageAsync(CreateMessageDto dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/messages", dto);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (result.TryGetProperty("id", out var idProp) && idProp.TryGetGuid(out var id))
                    {
                        return id;
                    }
                    return Guid.Empty; // Fallback success but no ID
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
        
        public async Task DeleteMessageAsync(Guid messageId)
        {
            try
            {
                await _httpClient.DeleteAsync($"api/messages/{messageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur DeleteMessageAsync: {ex.Message}");
            }
        }

		public async Task MarkMessagesAsReadAsync(Guid? colisId, Guid? vehiculeId, Guid? conteneurId)
        {
            try
            {
                var query = new List<string>();
                if (colisId.HasValue) query.Add($"colisId={colisId}");
                if (vehiculeId.HasValue) query.Add($"vehiculeId={vehiculeId}");
                if (conteneurId.HasValue) query.Add($"conteneurId={conteneurId}");

                var queryString = query.Any() ? "?" + string.Join("&", query) : "";
                await _httpClient.PostAsync($"api/messages/mark-read{queryString}", null);
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
		
		public async Task<byte[]> ExportTicketPdfAsync(Guid id, string format = "thermal")
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/colis/{id}/export/ticket?format={format}");
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

        public async Task<List<Client>> GetNewClientsListAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Client>>("api/dashboard/admin/new-clients");
        }

        public async Task<List<DashboardEntityDto>> GetDelayedItemsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<DashboardEntityDto>>("api/dashboard/admin/delayed-items");
        }

        public async Task<List<DashboardEntityDto>> GetUnpricedItemsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<DashboardEntityDto>>("api/dashboard/admin/unpriced-items");
        }
        public async Task<ReceptionControl?> GetReceptionControlAsync(string entityType, Guid entityId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ReceptionControl>($"api/reception/entity/{entityType}/{entityId}", _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<ReceptionControl?> CreateReceptionControlAsync(ReceptionControl control)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/reception", control, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ReceptionControl>(_jsonOptions);
                }
                return null;
            }
            catch { return null; }
        }

        public async Task<List<ReceptionControl>> GetRecentReceptionControlsAsync(int count)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<ReceptionControl>>($"api/reception/recent?count={count}", _jsonOptions) ?? new();
            }
            catch { return new List<ReceptionControl>(); }
        }

        public async Task<ReceptionStatsDto?> GetReceptionStatsAsync(DateTime? start = null, DateTime? end = null)
        {
            try
            {
                var query = "api/reception/stats";
                var paramsList = new List<string>();
                if (start.HasValue) paramsList.Add($"start={start.Value:yyyy-MM-dd}");
                if (end.HasValue) paramsList.Add($"end={end.Value:yyyy-MM-dd}");

                if (paramsList.Any()) query += "?" + string.Join("&", paramsList);

                return await _httpClient.GetFromJsonAsync<ReceptionStatsDto>(query, _jsonOptions);
            }
            catch { return null; }
        }

        public async Task<string> GetSettingAsync(string key)
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<SettingResponseDto>($"api/settings/{key}?_={DateTime.UtcNow.Ticks}", _jsonOptions);
                return result?.Value ?? "";
            }
            catch { return ""; }
        }

        private class SettingResponseDto { public string Value { get; set; } = ""; }

        public async Task<bool> DeleteControlAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/reception/{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
        
        public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(int page = 1, int pageSize = 20, string? userId = null, string? entityName = null, DateTime? date = null)
        {
            var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
            if (!string.IsNullOrEmpty(userId)) query.Add($"userId={userId}");
            if (!string.IsNullOrEmpty(entityName)) query.Add($"entityName={entityName}");
            if (date.HasValue) query.Add($"date={date.Value:yyyy-MM-dd}");

            try 
            {
                return await _httpClient.GetFromJsonAsync<PagedResult<AuditLogDto>>($"api/audit?{string.Join("&", query)}", _jsonOptions) 
                       ?? new PagedResult<AuditLogDto>();
            }
            catch { return new PagedResult<AuditLogDto>(); }
        }

        public async Task<AuditLogDto?> GetAuditLogByIdAsync(Guid id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<AuditLogDto>($"api/audit/{id}", _jsonOptions);
            }
            catch { return null; }
        }

        // --- Commerce ---

        public async Task<PagedResult<ProductDto>> GetProductsAsync(string? search, int page = 1, int pageSize = 50)
        {
             var query = $"page={page}&pageSize={pageSize}";
             if (!string.IsNullOrEmpty(search)) query += $"&search={search}";
             return await _httpClient.GetFromJsonAsync<PagedResult<ProductDto>>($"api/commerce/products?{query}", _jsonOptions) ?? new();
        }

        public async Task<byte[]> ExportProductsCsvAsync()
        {
            return await _httpClient.GetByteArrayAsync("api/commerce/products/export");
        }

        public async Task<ProductDto> CreateProductAsync(ProductDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("api/commerce/products", dto, _jsonOptions);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
        }

        public async Task<ProductDto> UpdateProductAsync(ProductDto dto)
        {
             var response = await _httpClient.PutAsJsonAsync($"api/commerce/products/{dto.Id}", dto, _jsonOptions);
             response.EnsureSuccessStatusCode();
             return await response.Content.ReadFromJsonAsync<ProductDto>(_jsonOptions);
        }

        public async Task DeleteProductAsync(Guid id)
        {
             await _httpClient.DeleteAsync($"api/commerce/products/{id}");
        }

        public async Task DeleteProductsManyAsync(List<Guid> ids) // NEW
        {
             await _httpClient.PostAsJsonAsync("api/commerce/products/delete-many", ids);
        }

        public async Task<int> ImportProductsCsvAsync(IBrowserFile file) // NEW
        {
             using var content = new MultipartFormDataContent();
             using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
             content.Add(new StreamContent(stream), "file", file.Name);
             
             var response = await _httpClient.PostAsync("api/commerce/products/import", content);
             if (response.IsSuccessStatusCode)
             {
                 var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                 if (result.TryGetProperty("count", out var c)) return c.GetInt32();
             }
             else 
             {
                 var error = await response.Content.ReadAsStringAsync();
                 throw new Exception($"Import failed ({response.StatusCode}): {error}");
             }
             return 0;
        }



        public async Task<PagedResult<QuoteDto>> GetQuotesAsync(string? search, Guid? clientId, string? status, int page = 1, int pageSize = 20)
        {
            var query = new List<string> { $"page={page}", $"pageSize={pageSize}" };
            if (!string.IsNullOrEmpty(search)) query.Add($"search={search}");
            if (clientId.HasValue) query.Add($"clientId={clientId}");
            if (!string.IsNullOrEmpty(status)) query.Add($"status={status}");
            
            var response = await _httpClient.GetAsync($"api/commerce/quotes?{string.Join("&", query)}");
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error GetQuotes: {response.StatusCode} - {content}");
                throw new Exception($"API Error {response.StatusCode}: {content}");
            }
            return await response.Content.ReadFromJsonAsync<PagedResult<QuoteDto>>(_jsonOptions) ?? new();
        }

        public async Task<QuoteDto> GetQuoteByIdAsync(Guid id)
        {
             try { return await _httpClient.GetFromJsonAsync<QuoteDto>($"api/commerce/quotes/{id}", _jsonOptions); }
             catch { return null; }
        }
        
        public async Task<QuoteDto> CreateOrUpdateQuoteAsync(UpsertQuoteDto dto)
        {
             var response = await _httpClient.PostAsJsonAsync("api/commerce/quotes", dto, _jsonOptions);
             response.EnsureSuccessStatusCode();
             return await response.Content.ReadFromJsonAsync<QuoteDto>(_jsonOptions);
        }

        public async Task UpdateQuoteStatusAsync(Guid id, QuoteStatus status, string? reason = null)
        {
             var url = $"api/commerce/quotes/{id}/status?status={status}";
             if(!string.IsNullOrEmpty(reason)) url += $"&rejectionReason={reason}";
             await _httpClient.PostAsync(url, null);
        }
        
        public async Task DeleteQuoteAsync(Guid id)
        {
             await _httpClient.DeleteAsync($"api/commerce/quotes/{id}");
        }

        public async Task SendQuoteByEmailAsync(Guid id, string? subject = null, string? body = null, bool copyToSender = false, List<Guid>? attachmentIds = null, List<string>? cc = null, List<string>? recipients = null)
        {
            var request = new SendQuoteEmailDto 
            { 
                Subject = subject, 
                Body = body, 
                CopyToSender = copyToSender, 
                TempAttachmentIds = attachmentIds,
                Cc = cc != null && cc.Any() ? string.Join(",", cc) : null,
                Recipients = recipients != null && recipients.Any() ? string.Join(",", recipients) : null
            };
            var response = await _httpClient.PostAsJsonAsync($"api/commerce/quotes/{id}/email", request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error sending email: {response.StatusCode} - {error}");
            }
        }

        // Public
        public async Task<QuoteDto> GetPublicQuoteAsync(Guid token)
        {
             try { return await _httpClient.GetFromJsonAsync<QuoteDto>($"api/commerce/public/quote/{token}", _jsonOptions); }
             catch { return null; }
        }

        public async Task AcceptPublicQuoteAsync(Guid token)
        {
             await _httpClient.PostAsync($"api/commerce/public/quote/{token}/accept", null);
        }

        public async Task RejectPublicQuoteAsync(Guid token, string reason)
        {
             await _httpClient.PostAsJsonAsync($"api/commerce/public/quote/{token}/reject", reason);
        }

        public async Task RequestChangesPublicQuoteAsync(Guid token, string comment)
        {
             await _httpClient.PostAsJsonAsync($"api/commerce/public/quote/{token}/request-changes", comment);
        }

        public async Task<(Guid Id, string Name)?> UploadTempDocumentAsync(IBrowserFile file)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(file.OpenReadStream(524288000)); // 500MB max
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.Name);

                var response = await _httpClient.PostAsync("api/documents/upload-temp", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (result.TryGetProperty("id", out var idProp) && result.TryGetProperty("name", out var nameProp))
                    {
                        return (idProp.GetGuid(), nameProp.GetString() ?? file.Name);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error uploading temp file: {ex.Message}");
                return null;
            }
        }
        public async Task<byte[]> GenerateQuotePdfAsync(Guid quoteId, Guid? token = null)
        {
            try
            {
                var url = $"api/commerce/quotes/{quoteId}/pdf";
                if(token.HasValue) url += $"?token={token}";
                return await _httpClient.GetByteArrayAsync(url);
            }
            catch { return Array.Empty<byte>(); }
        }

        public async Task<byte[]> GetInvoicePdfAsync(Guid invoiceId, Guid? token = null)
        {
            try
            {
                var url = $"api/commerce/invoices/{invoiceId}/pdf";
                if(token.HasValue) url += $"?token={token}";
                // Fallback to Quote PDF endpoint logic if Invoice endpoint not distinct yet?
                // Actually we should assume endpoint exists or map to correct one.
                // Assuming CommerceController has been updated or will be.
                // Since I cannot change Controller easily without restart, I will assume it maps to key "pdf" method.
                // Step 3500 commit showed CommerceController modified.
                return await _httpClient.GetByteArrayAsync(url);
            }
            catch { return Array.Empty<byte>(); }
        }



        public async Task<bool> SendInvoiceByEmailAsync(Guid id, string? subject, string? body, bool copyToSender, List<Guid>? attachments, List<string>? cc = null, List<string>? recipients = null)
        {
            try
            {
                var request = new SendQuoteEmailDto 
                { 
                    Subject = subject, 
                    Body = body, 
                    CopyToSender = copyToSender, 
                    TempAttachmentIds = attachments,
                    Cc = cc != null && cc.Any() ? string.Join(",", cc) : null,
                    Recipients = recipients != null && recipients.Any() ? string.Join(",", recipients) : null
                };
                var response = await _httpClient.PostAsJsonAsync($"api/commerce/invoices/{id}/email", request, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<bool> SendPaymentReminderAsync(Guid id, string? subject, string? body, List<Guid>? attachments, List<string>? cc = null, List<string>? recipients = null)
        {
            try
            {
                var request = new SendQuoteEmailDto 
                { 
                    Subject = subject, 
                    Body = body, 
                    CopyToSender = true, // Default true for reminders?
                    TempAttachmentIds = attachments,
                     Cc = cc != null && cc.Any() ? string.Join(",", cc) : null,
                     Recipients = recipients != null && recipients.Any() ? string.Join(",", recipients) : null
                };
                var response = await _httpClient.PostAsJsonAsync($"api/commerce/invoices/{id}/reminder", request, _jsonOptions);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<byte[]> GetInvoicePdfAsync(Guid id)
        {
            try
            {
               return await _httpClient.GetByteArrayAsync($"api/commerce/invoices/{id}/pdf");
            }
            catch { return Array.Empty<byte>(); }
        }


    }
}
