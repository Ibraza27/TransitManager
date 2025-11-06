using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        // --- MODIFICATION : Le constructeur n'injecte plus TransitContext ---
        public AuthController(IAuthenticationService authService, IJwtService jwtService)
        {
            _authService = authService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            // --- MODIFICATION : La logique est entièrement déléguée au service ---
            var authResult = await _authService.LoginAsync(request.Email, request.Password);

            if (!authResult.Success || authResult.User == null)
            {
                var errorResponse = new LoginResponseDto { Success = false, Message = authResult.ErrorMessage ?? "Email ou mot de passe incorrect." };
                return new ObjectResult(errorResponse) { StatusCode = StatusCodes.Status401Unauthorized };
            }

            var token = _jwtService.GenerateToken(authResult.User);
            return Ok(new LoginResponseDto { Success = true, Token = token, Message = "Connexion réussie." });
        }
    }
}