using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.DTOs;
using TransitManager.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using TransitManager.Infrastructure.Data;
using Microsoft.AspNetCore.Http; // <-- AJOUTER CE USING

namespace TransitManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly IJwtService _jwtService;
        private readonly TransitContext _context;

        public AuthController(IAuthenticationService authService, IJwtService jwtService, TransitContext context)
        {
            _authService = authService;
            _jwtService = jwtService;
            _context = context;
        }

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
		{
			// On passe directement l'email au service d'authentification, qui saura comment le gérer
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