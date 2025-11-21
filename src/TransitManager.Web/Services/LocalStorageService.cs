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
                Console.WriteLine($"[LocalStorage] Tentative de lecture de la clé: '{key}'");
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);

                if (string.IsNullOrEmpty(json))
                {
                    Console.WriteLine($"[LocalStorage] Clé '{key}' non trouvée ou vide.");
                    return default;
                }

                Console.WriteLine($"[LocalStorage] Données trouvées pour la clé '{key}', longueur: {json.Length}. Désérialisation...");
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalStorage] Erreur lors de la lecture de la clé '{key}': {ex.Message}");
                return default;
            }
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                Console.WriteLine($"[LocalStorage] Tentative d'écriture de la clé: '{key}', longueur des données: {json.Length}");
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
                Console.WriteLine($"[LocalStorage] Écriture de la clé '{key}' terminée avec succès.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalStorage] Erreur lors de l'écriture de la clé '{key}': {ex.Message}");
            }
        }

        public async Task RemoveItemAsync(string key)
        {
            try
            {
                Console.WriteLine($"[LocalStorage] Tentative de suppression de la clé: '{key}'");
                await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
                Console.WriteLine($"[LocalStorage] Suppression de la clé '{key}' terminée.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LocalStorage] Erreur lors de la suppression de la clé '{key}': {ex.Message}");
            }
        }
    }
}