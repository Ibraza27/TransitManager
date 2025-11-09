using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IJwtService _jwtService;

        public AuthController(IAuthenticationService authService, IJwtService jwtService)
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
    }
}