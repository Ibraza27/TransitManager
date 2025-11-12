using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using TransitManager.Web.Services;

namespace TransitManager.Web.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;
        private bool _isInitialized = false;
        private AuthenticationState _cachedState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        public CustomAuthenticationStateProvider(ILocalStorageService localStorage, IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
        {
            _localStorage = localStorage;
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClient;
        }

		public override async Task<AuthenticationState> GetAuthenticationStateAsync()
		{
			Console.WriteLine("🔐 [AuthProvider] === DÉBUT GetAuthenticationStateAsync ===");
			
			try
			{
				// Si déjà initialisé, retourner l'état en cache
				if (_isInitialized)
				{
					Console.WriteLine($"🔐 [AuthProvider] ✅ État déjà initialisé. Authentifié: {_cachedState.User.Identity?.IsAuthenticated}");
					return _cachedState;
				}

				Console.WriteLine("🔐 [AuthProvider] 📍 Étape 1: Vérification de l'utilisateur serveur...");
				var httpContext = _httpContextAccessor.HttpContext;
				
				if (httpContext?.User.Identity?.IsAuthenticated == true)
				{
					Console.WriteLine($"🔐 [AuthProvider] ✅ Utilisateur authentifié via COOKIE SERVEUR: {httpContext.User.Identity.Name}");
					_cachedState = new AuthenticationState(httpContext.User);
					_isInitialized = true;
					return _cachedState;
				}
				else
				{
					Console.WriteLine("🔐 [AuthProvider] ❌ Aucun utilisateur serveur trouvé");
				}

				Console.WriteLine("🔐 [AuthProvider] 📍 Étape 2: Tentative de lecture du token JWT du localStorage...");
				var token = await _localStorage.GetItemAsync<string>("authToken");
				
				if (!string.IsNullOrEmpty(token))
				{
					Console.WriteLine("🔐 [AuthProvider] ✅ Token JWT trouvé dans localStorage");
					var identity = CreateIdentityFromToken(token);
					if (identity.IsAuthenticated)
					{
						_cachedState = new AuthenticationState(new ClaimsPrincipal(identity));
						_isInitialized = true;
						
						Console.WriteLine($"🔐 [AuthProvider] ✅ Identité créée: {identity.Name}, Rôle: {identity.FindFirst(ClaimTypes.Role)?.Value}");
						return _cachedState;
					}
					else
					{
						Console.WriteLine("🔐 [AuthProvider] ❌ Token JWT invalide ou expiré");
					}
				}
				else
				{
					Console.WriteLine("🔐 [AuthProvider] ❌ Aucun token trouvé dans localStorage");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"🔐 [AuthProvider] ⚠️ Exception lors de l'initialisation: {ex.Message}");
				Console.WriteLine($"🔐 [AuthProvider] StackTrace: {ex.StackTrace}");
			}

			Console.WriteLine("🔐 [AuthProvider] 🔄 Retour état ANONYME (initialisation en cours)");
			return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
		}

		public async Task InitializeAsync()
		{
			Console.WriteLine("🔐 [AuthProvider] === DÉBUT InitializeAsync ===");
			
			try
			{
				if (_isInitialized)
				{
					Console.WriteLine("🔐 [AuthProvider] ℹ️  Déjà initialisé, sortie");
					return;
				}

				Console.WriteLine("🔐 [AuthProvider] 📍 Tentative de lecture du token JWT...");
				var token = await _localStorage.GetItemAsync<string>("authToken");
				
				if (!string.IsNullOrEmpty(token))
				{
					Console.WriteLine("🔐 [AuthProvider] ✅ Token trouvé lors de l'initialisation");
					var identity = CreateIdentityFromToken(token);
					
					if (identity.IsAuthenticated)
					{
						var newState = new AuthenticationState(new ClaimsPrincipal(identity));
						_cachedState = newState;
						_isInitialized = true;
						
						Console.WriteLine($"🔐 [AuthProvider] 🔄 Notification changement état: {newState.User.Identity.Name}");
						NotifyAuthenticationStateChanged(Task.FromResult(newState));
					}
					else
					{
						Console.WriteLine("🔐 [AuthProvider] ⚠️ Token invalide - état non authentifié");
						_isInitialized = true;
					}
				}
				else
				{
					Console.WriteLine("🔐 [AuthProvider] ❌ Aucun token trouvé lors de l'initialisation");
					_isInitialized = true;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"🔐 [AuthProvider] 💥 Erreur lors de l'initialisation: {ex.Message}");
				Console.WriteLine($"🔐 [AuthProvider] StackTrace: {ex.StackTrace}");
				_isInitialized = true;
			}
			
			Console.WriteLine("🔐 [AuthProvider] === FIN InitializeAsync ===");
		}

		// NOUVELLE MÉTHODE : Initialisation différée après le rendu
		public async Task<bool> InitializeAfterRenderAsync()
		{
			Console.WriteLine("🔐 [AuthProvider] === INITIALISATION DIFFÉRÉE APRÈS RENDU ===");
			
			try
			{
				if (_isInitialized)
				{
					Console.WriteLine("🔐 [AuthProvider] ℹ️  Déjà initialisé, sortie");
					return _cachedState.User.Identity?.IsAuthenticated == true;
				}

				Console.WriteLine("🔐 [AuthProvider] 📍 Lecture du token JWT après rendu...");
				var token = await _localStorage.GetItemAsync<string>("authToken");
				
				if (!string.IsNullOrEmpty(token))
				{
					Console.WriteLine("🔐 [AuthProvider] ✅ Token trouvé lors de l'initialisation différée");
					var identity = CreateIdentityFromToken(token);
					
					if (identity.IsAuthenticated)
					{
						var newState = new AuthenticationState(new ClaimsPrincipal(identity));
						_cachedState = newState;
						_isInitialized = true;
						
						Console.WriteLine($"🔐 [AuthProvider] 🔄 Notification changement état: {newState.User.Identity.Name}");
						NotifyAuthenticationStateChanged(Task.FromResult(newState));
						return true;
					}
					else
					{
						Console.WriteLine("🔐 [AuthProvider] ⚠️ Token invalide - état non authentifié");
						_isInitialized = true;
						return false;
					}
				}
				else
				{
					Console.WriteLine("🔐 [AuthProvider] ❌ Aucun token trouvé lors de l'initialisation différée");
					_isInitialized = true;
					return false;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"🔐 [AuthProvider] 💥 Erreur lors de l'initialisation différée: {ex.Message}");
				Console.WriteLine($"🔐 [AuthProvider] StackTrace: {ex.StackTrace}");
				_isInitialized = true;
				return false;
			}
		}

		// NOUVELLE MÉTHODE : Pour forcer l'initialisation après le rendu
		public async Task<bool> ForceReinitializeAsync()
		{
			Console.WriteLine("🔐 [AuthProvider] === FORCE REINITIALIZATION ===");
			
			try
			{
				_isInitialized = false;
				_cachedState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
				
				Console.WriteLine("🔐 [AuthProvider] 📍 Lecture forcée du token...");
				var token = await _localStorage.GetItemAsync<string>("authToken");
				
				if (!string.IsNullOrEmpty(token))
				{
					Console.WriteLine("🔐 [AuthProvider] ✅ Token trouvé lors de la réinitialisation forcée");
					var identity = CreateIdentityFromToken(token);
					
					if (identity.IsAuthenticated)
					{
						var newState = new AuthenticationState(new ClaimsPrincipal(identity));
						_cachedState = newState;
						_isInitialized = true;
						
						Console.WriteLine($"🔐 [AuthProvider] 🔄 Notification changement état: {newState.User.Identity.Name}");
						NotifyAuthenticationStateChanged(Task.FromResult(newState));
						return true;
					}
				}
				
				Console.WriteLine("🔐 [AuthProvider] ❌ Aucun token valide trouvé");
				_isInitialized = true;
				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"🔐 [AuthProvider] 💥 Erreur lors de la réinitialisation forcée: {ex.Message}");
				_isInitialized = true;
				return false;
			}
		}


		// NOUVELLE MÉTHODE : Version améliorée avec gestion d'erreurs
		private ClaimsIdentity CreateIdentityFromToken(string token)
		{
			Console.WriteLine("🔐 [AuthProvider] === DÉBUT CreateIdentityFromToken ===");
			
			try
			{
				Console.WriteLine("🔐 [AuthProvider] 📍 Décodage du token JWT...");
				var handler = new JwtSecurityTokenHandler();
				
				if (!handler.CanReadToken(token))
				{
					Console.WriteLine("🔐 [AuthProvider] ❌ Token JWT invalide - impossible de lire");
					return new ClaimsIdentity();
				}

				var jwtToken = handler.ReadJwtToken(token);
				var claims = jwtToken.Claims.ToList();

				Console.WriteLine($"🔐 [AuthProvider] 📍 Token décodé - {claims.Count} claims trouvés");
				
				// Vérifier l'expiration
				var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
				if (expClaim != null)
				{
					var expDateTime = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value));
					if (expDateTime < DateTimeOffset.UtcNow)
					{
						Console.WriteLine("🔐 [AuthProvider] ❌ Token JWT expiré");
						return new ClaimsIdentity();
					}
					Console.WriteLine($"🔐 [AuthProvider] ✅ Token valide - expiration: {expDateTime}");
				}

				var identity = new ClaimsIdentity(claims, "jwt");
				Console.WriteLine($"🔐 [AuthProvider] ✅ Identité créée avec succès - Authentifié: {identity.IsAuthenticated}");
				
				return identity;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"🔐 [AuthProvider] 💥 Erreur décodage token: {ex.Message}");
				Console.WriteLine($"🔐 [AuthProvider] StackTrace: {ex.StackTrace}");
				return new ClaimsIdentity();
			}
			finally
			{
				Console.WriteLine("🔐 [AuthProvider] === FIN CreateIdentityFromToken ===");
			}
		}


		public async Task MarkUserAsAuthenticated(string token)
		{
			Console.WriteLine("🔐 [AuthProvider] === MarkUserAsAuthenticated ===");
			
			try
			{
				// CORRECTION : Écriture sécurisée dans le localStorage
				Console.WriteLine("🔐 [AuthProvider] 📍 Tentative d'écriture du token dans le localStorage...");
				await _localStorage.SetItemAsync("authToken", token);
				Console.WriteLine("🔐 [AuthProvider] ✅ Token écrit avec succès dans le localStorage");
				
				var identity = CreateIdentityFromToken(token);
				var user = new ClaimsPrincipal(identity);
				_cachedState = new AuthenticationState(user);
				_isInitialized = true;
				
				Console.WriteLine($"🔐 [AuthProvider] ✅ Utilisateur marqué comme authentifié: {user.Identity?.Name}");
				NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
			}
			catch (Exception ex)
			{
				Console.WriteLine($"🔐 [AuthProvider] ❌ Erreur lors de MarkUserAsAuthenticated: {ex.Message}");
				
				// CORRECTION : Même en cas d'erreur de localStorage, on maintient l'état authentifié
				var identity = CreateIdentityFromToken(token);
				var user = new ClaimsPrincipal(identity);
				_cachedState = new AuthenticationState(user);
				_isInitialized = true;
				
				Console.WriteLine($"🔐 [AuthProvider] ✅ État authentifié maintenu malgré l'erreur localStorage: {user.Identity?.Name}");
				NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
			}
		}

        public async Task MarkUserAsLoggedOut()
        {
            Console.WriteLine("🔐 [AuthProvider] === MarkUserAsLoggedOut ===");
            await _localStorage.RemoveItemAsync("authToken");
            
            _cachedState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            _isInitialized = true;
            
            Console.WriteLine("🔐 [AuthProvider] ✅ Utilisateur marqué comme déconnecté");
            NotifyAuthenticationStateChanged(Task.FromResult(_cachedState));
        }
		
		public async Task<bool> WaitForInitializationAsync(TimeSpan timeout)
		{
			Console.WriteLine("🔐 [AuthProvider] === Attente de l'initialisation ===");
			
			var startTime = DateTime.UtcNow;
			
			while (!_isInitialized && (DateTime.UtcNow - startTime) < timeout)
			{
				Console.WriteLine("🔐 [AuthProvider] ⏳ Initialisation pas encore terminée, attente...");
				await Task.Delay(100);
				
				// Forcer une nouvelle vérification
				await GetAuthenticationStateAsync();
			}
			
			Console.WriteLine($"🔐 [AuthProvider] ✅ Initialisation terminée: {_isInitialized}");
			return _isInitialized;
		}
    }
}