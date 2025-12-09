using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
// On garde le using global pour les autres interfaces comme IJwtService
using TransitManager.Core.Interfaces;
using Microsoft.AspNetCore.Authorization; 

namespace TransitManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // MODIFICATION 1 : Utiliser le nom complet de VOTRE interface
        private readonly TransitManager.Core.Interfaces.IAuthenticationService _authService;
        private readonly IJwtService _jwtService;

        // MODIFICATION 2 : Utiliser le nom complet de VOTRE interface dans le constructeur
        public AuthController(TransitManager.Core.Interfaces.IAuthenticationService authService, IJwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var requestBody = await reader.ReadToEndAsync();

                Console.WriteLine($"[API] Corps de la requête reçu : {requestBody}");

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<LoginRequestDto>(requestBody, options);

                if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    Console.WriteLine("[API] Échec de la désérialisation ou données vides.");
                    return BadRequest("Les données de connexion sont invalides ou manquantes.");
                }

                Console.WriteLine($"[API] Tentative de connexion pour: {request.Email}");

                var authResult = await _authService.LoginAsync(request.Email, request.Password);

                if (!authResult.Success || authResult.User == null)
                {
                    var errorResponse = new LoginResponseDto { 
                        Success = false, 
                        Message = authResult.ErrorMessage ?? "Email ou mot de passe incorrect." 
                    };
                    return new ObjectResult(errorResponse) { StatusCode = StatusCodes.Status401Unauthorized };
                }

                var token = _jwtService.GenerateToken(authResult.User);
                return Ok(new LoginResponseDto { 
                    Success = true, 
                    Token = token, 
                    Message = "Connexion réussie." 
                });
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"[API] Erreur de désérialisation JSON : {jsonEx.Message}");
                return BadRequest("Le format des données envoyées est incorrect.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Erreur inattendue dans le contrôleur de login : {ex.Message}");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }
		
		[HttpPost("login-with-cookie")]
		public async Task<IActionResult> LoginWithCookie([FromBody] LoginRequestDto request)
		{
			Console.WriteLine("🛂 [API - LoginWithCookie] === DÉBUT ===");
			if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
			{
				Console.WriteLine("🛂 [API - LoginWithCookie] ❌ Données de connexion invalides.");
				return BadRequest("Les données de connexion sont invalides.");
			}
			var authResult = await _authService.LoginAsync(request.Email, request.Password);
			if (!authResult.Success || authResult.User == null)
			{
				Console.WriteLine("🛂 [API - LoginWithCookie] ❌ Échec de l'authentification.");

				// Si c'est un problème d'email non confirmé, on renvoie un 403 (Forbidden) spécifique
				if (authResult.IsEmailUnconfirmed)
				{
					return StatusCode(403, new {
						message = "Email non confirmé",
						isEmailUnconfirmed = true
					});
				}
				// ... retour erreur classique (Unauthorized) ...
				return Unauthorized(new {
					message = authResult.ErrorMessage ?? "Email ou mot de passe incorrect.",
					lockoutEnd = authResult.LockoutEnd
				});
			}
			Console.WriteLine("🛂 [API - LoginWithCookie] ✅ Authentification réussie. Création des claims...");
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, authResult.User.Id.ToString()),
				new Claim(ClaimTypes.Name, authResult.User.NomComplet),
				new Claim(ClaimTypes.Email, authResult.User.Email),
				new Claim(ClaimTypes.Role, authResult.User.Role.ToString()),
			};
			// === DÉBUT DE L'AJOUT ===
			// Si l'utilisateur est lié à un client, on ajoute cette information dans les claims.
			if (authResult.User.ClientId.HasValue)
			{
				var clientIdClaim = new Claim("client_id", authResult.User.ClientId.Value.ToString());
				claims.Add(clientIdClaim);
				Console.WriteLine($"🛂 [API - LoginWithCookie]   -> Claim ClientID ajouté: {clientIdClaim.Type} = {clientIdClaim.Value}");
			}
			// === FIN DE L'AJOUT ===
			foreach (var claim in claims)
			{
				Console.WriteLine($"🛂 [API - LoginWithCookie]   -> Claim ajouté: {claim.Type} = {claim.Value}");
			}
			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var authProperties = new AuthenticationProperties
			{
				IsPersistent = true,
				ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
			};

			Console.WriteLine($"🛂 [API - LoginWithCookie] 🍪 Propriétés du cookie: IsPersistent={authProperties.IsPersistent}, ExpiresUtc={authProperties.ExpiresUtc}");
			try
			{
				// Crée le cookie chiffré et l'ajoute à la réponse HTTP
				await HttpContext.SignInAsync(
					CookieAuthenticationDefaults.AuthenticationScheme,
					new ClaimsPrincipal(claimsIdentity),
					authProperties);

				Console.WriteLine("🛂 [API - LoginWithCookie] ✅ HttpContext.SignInAsync exécuté avec succès. Le cookie devrait être dans la réponse.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"🛂 [API - LoginWithCookie] 💥 ERREUR lors de HttpContext.SignInAsync: {ex.Message}");
				// Ne pas bloquer le login même si le cookie échoue, le token JWT reste une solution de repli.
			}
			// Génère également le token JWT pour le localStorage
			var token = _jwtService.GenerateToken(authResult.User);
			Console.WriteLine("🛂 [API - LoginWithCookie] ✅ Token JWT généré. Envoi de la réponse OK.");
			Console.WriteLine("🛂 [API - LoginWithCookie] === FIN ===");
			return Ok(new { success = true, token = token, message = "Connexion réussie." });
		}


		[HttpPost("resend-confirmation")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationDto request)
        {
            Console.WriteLine("🚀 [API] REÇU : Requête resend-confirmation.");

            if (request == null)
            {
                Console.WriteLine("❌ [API] ERREUR : Le corps de la requête est vide ou mal formé.");
                return BadRequest("Requête invalide.");
            }

            Console.WriteLine($"🔍 [API] Email reçu : '{request.Email}'");

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                Console.WriteLine("❌ [API] ERREUR : L'email est vide.");
                return BadRequest("L'email est requis.");
            }

            try
            {
                // Appel du service
                await _authService.ResendConfirmationEmailAsync(request.Email);
                
                Console.WriteLine("✅ [API] SUCCÈS : Service exécuté sans erreur.");
                return Ok(new { message = "Email renvoyé." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 [API] EXCEPTION : {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, "Erreur interne API.");
            }
        }

		
        // === AJOUTER CETTE MÉTHODE ===
        [Authorize] // Seuls les utilisateurs connectés peuvent se déconnecter
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Cette méthode supprime le cookie d'authentification côté serveur
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                Console.WriteLine("[API] Déconnexion réussie, cookie supprimé.");
                return Ok(new { success = true, message = "Déconnexion réussie." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Erreur lors de la déconnexion : {ex.Message}");
                return StatusCode(500, "Une erreur interne est survenue lors de la déconnexion.");
            }
        }
        // === FIN DE L'AJOUT ===
		
		[HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterClientRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest("Données invalides.");

            var result = await _authService.RegisterClientAsync(request);
            
            if (result.Success)
            {
                return Ok(new { Message = "Compte créé avec succès. Vous pouvez vous connecter." });
            }
            else
            {
                return BadRequest(new { Message = result.ErrorMessage });
            }
        }
		
		[HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            await _authService.RequestPasswordResetAsync(email);
            // On renvoie toujours OK pour ne pas divulguer si l'email existe ou non (sécurité)
            return Ok(new { message = "Si cet email existe, un lien a été envoyé." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            var result = await _authService.ResetPasswordWithTokenAsync(request.Email, request.Token, request.NewPassword);
            if (result) return Ok(new { message = "Mot de passe réinitialisé avec succès." });
            return BadRequest("Lien invalide ou expiré.");
        }

		[HttpPost("verify-email")]
        [AllowAnonymous] // Important : L'utilisateur n'est pas forcément connecté quand il clique sur le lien
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto request)
        {
            Console.WriteLine($"🚀 [API] Réception demande Validation Email.");

            if (request == null)
            {
                Console.WriteLine("❌ [API] Request est NULL.");
                return BadRequest("Requête invalide.");
            }

            Console.WriteLine($"🔍 [API] Email: {request.Email}, Token: {request.Token}");

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Token))
            {
                Console.WriteLine("❌ [API] Email ou Token vide.");
                return BadRequest("Email et Token sont requis.");
            }

            try 
            {
                var result = await _authService.VerifyEmailAsync(request.Email, request.Token);
                
                if (result) 
                {
                    Console.WriteLine("✅ [API] Email confirmé avec succès !");
                    return Ok(new { message = "Email confirmé." });
                }
                
                Console.WriteLine("⚠️ [API] Le service a retourné false (Token invalide ou expiré).");
                return BadRequest("Lien invalide ou expiré.");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"💥 [API] Erreur: {ex.Message}");
                return StatusCode(500, "Erreur interne.");
            }
        }
		
    }
	
}