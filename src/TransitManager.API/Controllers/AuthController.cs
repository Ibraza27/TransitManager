using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
// On garde le using global pour les autres interfaces comme IJwtService
using TransitManager.Core.Interfaces;

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
			if (request == null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
			{
				return BadRequest("Les données de connexion sont invalides.");
			}

			var authResult = await _authService.LoginAsync(request.Email, request.Password);

			if (!authResult.Success || authResult.User == null)
			{
				return Unauthorized(new { message = authResult.ErrorMessage ?? "Email ou mot de passe incorrect." });
			}

			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, authResult.User.Id.ToString()),
				new Claim(ClaimTypes.Name, authResult.User.NomComplet),
				new Claim(ClaimTypes.Email, authResult.User.Email),
				new Claim(ClaimTypes.Role, authResult.User.Role.ToString()),
			};

			var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
			var authProperties = new AuthenticationProperties
			{
				IsPersistent = true, // Le cookie persiste après la fermeture du navigateur
				ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
			};

			// Crée le cookie chiffré et l'ajoute à la réponse HTTP
			await HttpContext.SignInAsync(
				CookieAuthenticationDefaults.AuthenticationScheme,
				new ClaimsPrincipal(claimsIdentity),
				authProperties);

			// Génère également le token JWT pour le localStorage
			var token = _jwtService.GenerateToken(authResult.User);

			return Ok(new { success = true, token = token, message = "Connexion réussie." });
		}
		
    }
}