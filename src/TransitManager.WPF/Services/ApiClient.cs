// src/TransitManager.WPF/Services/ApiClient.cs

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;

namespace TransitManager.WPF.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;

        public ApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("API");
        }

        public async Task<PortalAccessResult> CreateOrResetPortalAccess(Guid clientId)
        {
            var request = new { ClientId = clientId };
            var response = await _httpClient.PostAsJsonAsync("api/users/create-portal-access", request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error creating portal access: {response.StatusCode} - {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<PortalAccessResult>();
            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize the portal access result.");
            }
            return result;
        }
    }
}