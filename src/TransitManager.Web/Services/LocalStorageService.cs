using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading.Tasks;

namespace TransitManager.Web.Services
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
                if (string.IsNullOrEmpty(json))
                    return default;

                return JsonSerializer.Deserialize<T>(json);
            }
            // Cette exception est levée pendant le pré-rendu statique. C'est normal.
            // On la capture et on retourne la valeur par défaut (null), ce qui signifie "non connecté".
            catch (InvalidOperationException)
            {
                return default;
            }
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
            }
            catch (InvalidOperationException)
            {
                // Ignorer l'erreur si on essaie d'écrire pendant le pré-rendu
            }
        }

        public async Task RemoveItemAsync(string key)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch (InvalidOperationException)
            {
                // Ignorer l'erreur si on essaie d'écrire pendant le pré-rendu
            }
        }
    }
}