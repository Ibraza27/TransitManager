using Microsoft.AspNetCore.Http; // AJOUTER CE USING
using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading.Tasks;

namespace TransitManager.Web.Services
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly IHttpContextAccessor _httpContextAccessor; // AJOUTER CE CHAMP

        // MODIFIER LE CONSTRUCTEUR
        public LocalStorageService(IJSRuntime jsRuntime, IHttpContextAccessor httpContextAccessor)
        {
            _jsRuntime = jsRuntime;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            // ---- DÉBUT DE LA LOGIQUE DE CONTRÔLE ----
            // Si HttpContext est non nul, cela signifie que nous sommes en phase de pré-rendu statique sur le serveur.
            if (_httpContextAccessor.HttpContext != null)
            {
                // Dans ce mode, aucun accès au JS n'est possible. On retourne la valeur par défaut.
                return default;
            }
            // ---- FIN DE LA LOGIQUE DE CONTRÔLE ----

            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json);
        }

        // Les méthodes SetItemAsync et RemoveItemAsync n'ont pas besoin d'être modifiées
        // car elles ne sont appelées qu'après une interaction de l'utilisateur (donc en mode interactif).
        public async Task SetItemAsync<T>(string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
        }

        public async Task RemoveItemAsync(string key)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
    }
}