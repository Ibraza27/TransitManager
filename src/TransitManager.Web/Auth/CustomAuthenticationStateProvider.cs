using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using TransitManager.Web.Services;
using System.Collections.Generic;

namespace TransitManager.Web.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }


		public override async Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			try
			{
				Console.WriteLine("[AuthProvider] GetAuthenticationStateAsync - Lecture du token...");
				var token = await _localStorage.GetItemAsync<string>("authToken");

				if (string.IsNullOrWhiteSpace(token))
				{
					Console.WriteLine("[AuthProvider] Aucun token trouvé - utilisateur anonyme");
					return new AuthenticationState(_anonymous);
				}

				Console.WriteLine($"[AuthProvider] Token trouvé, longueur: {token.Length}");

				// Vérifier si le token est expiré
				if (IsTokenExpired(token))
				{
					Console.WriteLine("[AuthProvider] Token expiré - déconnexion");
					await MarkUserAsLoggedOut();
					return new AuthenticationState(_anonymous);
				}

				var claimsPrincipal = CreateClaimsPrincipalFromToken(token);
				Console.WriteLine($"[AuthProvider] Utilisateur authentifié: {claimsPrincipal.Identity?.Name}");
				
				return new AuthenticationState(claimsPrincipal);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[AuthProvider] Erreur GetAuthenticationStateAsync: {ex.Message}");
				return new AuthenticationState(_anonymous);
			}
		}

        public async Task MarkUserAsAuthenticated(string token)
        {
            try
            {
                // Sauvegarder d'abord le token
                await _localStorage.SetItemAsync("authToken", token);
                
                // Ensuite notifier le changement d'état
                var claimsPrincipal = CreateClaimsPrincipalFromToken(token);
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthProvider] Erreur MarkUserAsAuthenticated: {ex.Message}");
            }
        }

        public async Task MarkUserAsLoggedOut()
        {
            try
            {
                await _localStorage.RemoveItemAsync("authToken");
                NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthProvider] Erreur MarkUserAsLoggedOut: {ex.Message}");
            }
        }

        private ClaimsPrincipal CreateClaimsPrincipalFromToken(string token)
        {
            try
            {
                var claims = ParseClaimsFromJwt(token);
                var identity = new ClaimsIdentity(claims, "jwtAuth");
                return new ClaimsPrincipal(identity);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthProvider] Erreur création ClaimsPrincipal: {ex.Message}");
                return _anonymous;
            }
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(jwt);
                return token.Claims;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuthProvider] Erreur parsing JWT: {ex.Message}");
                return new List<Claim>();
            }
        }

        private bool IsTokenExpired(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.ValidTo < DateTime.UtcNow;
            }
            catch
            {
                return true;
            }
        }
    }
}