// src/TransitManager.Web/Auth/CustomAuthenticationStateProvider.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using TransitManager.Web.Services;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace TransitManager.Web.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        // Cet état représente l'utilisateur anonyme. Il est mis en cache.
        private readonly AuthenticationState _anonymous = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        public CustomAuthenticationStateProvider(ILocalStorageService localStorage, IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
        {
            _localStorage = localStorage;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
        }
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var sw = Stopwatch.StartNew();
            Console.WriteLine("🔐 [AuthProvider] === DÉBUT GetAuthenticationStateAsync ===");
            try
            {
                // PRIORITÉ 1: Authentification par Cookie (côté serveur)
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                {
                    Console.WriteLine("🔐 [AuthProvider] ⚠️ HttpContext est NULL. Impossible de vérifier le cookie. Passage au token.");
                }
                else
                {
                    Console.WriteLine("🔐 [AuthProvider] 🍪 HttpContext est disponible. Vérification de l'identité de l'utilisateur...");
                    // Log des cookies reçus par le serveur Blazor
                    if (httpContext.Request.Cookies.Any())
                    {
                        foreach (var cookie in httpContext.Request.Cookies)
                        {
                            Console.WriteLine($"🔐 [AuthProvider] 🍪 Cookie reçu par le serveur: {cookie.Key}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("🔐 [AuthProvider] 🍪 Le serveur n'a reçu aucun cookie du navigateur.");
                    }
                    if (httpContext.User?.Identity?.IsAuthenticated == true)
                    {
                        Console.WriteLine($"🔐 [AuthProvider] ✅ SUCCÈS via Cookie. Utilisateur: {httpContext.User.Identity.Name}");
                        foreach (var claim in httpContext.User.Claims)
                        {
                            Console.WriteLine($"🔐 [AuthProvider]   -> Claim: {claim.Type} = {claim.Value}");
                        }
                        sw.Stop();
                        Console.WriteLine($"🔐 [AuthProvider] === FIN GetAuthenticationStateAsync ({sw.ElapsedMilliseconds}ms) ===");
                        return new AuthenticationState(new ClaimsPrincipal(httpContext.User.Identity));
                    }
                    else
                    {
                        Console.WriteLine("🔐 [AuthProvider] ❌ L'identité de l'utilisateur via HttpContext N'EST PAS authentifiée.");
                    }
                }
                // PRIORITÉ 2: Authentification par Token JWT (côté client, après interactivité)
                try
                {
                    Console.WriteLine("🔐 [AuthProvider] ℹ️ Pas de cookie, tentative de lecture du token JWT du localStorage...");
                    var token = await _localStorage.GetItemAsync<string>("authToken");
                    if (string.IsNullOrEmpty(token))
                    {
                        Console.WriteLine("🔐 [AuthProvider] ❌ Aucun token JWT trouvé. Retour état ANONYME.");
                        sw.Stop();
                        Console.WriteLine($"🔐 [AuthProvider] === FIN GetAuthenticationStateAsync ({sw.ElapsedMilliseconds}ms) ===");
                        return _anonymous;
                    }
                    var identity = CreateIdentityFromToken(token);
                    if (identity.IsAuthenticated)
                    {
                        Console.WriteLine($"🔐 [AuthProvider] ✅ SUCCÈS via Token JWT. Utilisateur: {identity.Name}");
                        sw.Stop();
                        Console.WriteLine($"🔐 [AuthProvider] === FIN GetAuthenticationStateAsync ({sw.ElapsedMilliseconds}ms) ===");
                        return new AuthenticationState(new ClaimsPrincipal(identity));
                    }
                    else
                    {
                        Console.WriteLine("🔐 [AuthProvider] ❌ Token JWT invalide ou expiré. Retour état ANONYME.");
                        sw.Stop();
                        Console.WriteLine($"🔐 [AuthProvider] === FIN GetAuthenticationStateAsync ({sw.ElapsedMilliseconds}ms) ===");
                        return _anonymous;
                    }
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop"))
                {
                    Console.WriteLine("🔐 [AuthProvider] ⚠️ JS Interop non dispo (pré-rendu). C'est normal. Retour état ANONYME.");
                    sw.Stop();
                    Console.WriteLine($"🔐 [AuthProvider] === FIN GetAuthenticationStateAsync ({sw.ElapsedMilliseconds}ms) ===");
                    return _anonymous;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔐 [AuthProvider] 💥 Erreur fatale dans GetAuthenticationStateAsync: {ex.Message}");
                sw.Stop();
                Console.WriteLine($"🔐 [AuthProvider] === FIN GetAuthenticationStateAsync ({sw.ElapsedMilliseconds}ms) ===");
                return _anonymous;
            }
        }
        public async Task MarkUserAsAuthenticated(string token)
        {
            Console.WriteLine("🔐 [AuthProvider] === MarkUserAsAuthenticated ===");
            try
            {
                await _localStorage.SetItemAsync("authToken", token);
                Console.WriteLine("🔐 [AuthProvider] ✅ Token écrit dans localStorage.");
                var identity = CreateIdentityFromToken(token);
                var user = new ClaimsPrincipal(identity);
                Console.WriteLine($"🔐 [AuthProvider] ✅ Utilisateur marqué comme authentifié: {user.Identity?.Name}");
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔐 [AuthProvider] ❌ Erreur dans MarkUserAsAuthenticated: {ex.Message}");
            }
        }
        public async Task MarkUserAsLoggedOut()
        {
            Console.WriteLine("🔐 [AuthProvider] === MarkUserAsLoggedOut ===");
            try
            {
                await _localStorage.RemoveItemAsync("authToken");
                Console.WriteLine("🔐 [AuthProvider] ✅ Token supprimé du localStorage.");
                // IMPORTANT : Il faudra aussi appeler un endpoint API pour supprimer le cookie.
                // Pour l'instant, on notifie simplement le changement d'état.

                Console.WriteLine("🔐 [AuthProvider] ✅ Utilisateur marqué comme déconnecté.");
                NotifyAuthenticationStateChanged(Task.FromResult(_anonymous));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔐 [AuthProvider] ❌ Erreur dans MarkUserAsLoggedOut: {ex.Message}");
            }
        }
        private ClaimsIdentity CreateIdentityFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    Console.WriteLine("🔐 [AuthProvider] ❌ Token invalide (format incorrect).");
                    return new ClaimsIdentity();
                }
                var jwtToken = handler.ReadJwtToken(token);
                var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
                if (expClaim != null)
                {
                    var expDateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value));
                    if (expDateTime < DateTimeOffset.UtcNow)
                    {
                        Console.WriteLine("🔐 [AuthProvider] ❌ Token expiré.");
                        return new ClaimsIdentity();
                    }
                }
                // Appliquer le token à l'en-tête HttpClient pour les futurs appels API
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return new ClaimsIdentity(jwtToken.Claims, "jwt");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔐 [AuthProvider] 💥 Erreur décodage token: {ex.Message}");
                return new ClaimsIdentity();
            }
        }
    }
}
