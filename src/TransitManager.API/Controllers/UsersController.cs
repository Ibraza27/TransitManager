// src/TransitManager.API/Controllers/UsersController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IAuthenticationService _authService;

        public UsersController(IAuthenticationService authService)
        {
            _authService = authService;
        }

        public class CreatePortalAccessRequest
        {
            public Guid ClientId { get; set; }
        }

        [HttpPost("create-portal-access")]
        public async Task<IActionResult> CreatePortalAccess([FromBody] CreatePortalAccessRequest request)
        {
            if (request.ClientId == Guid.Empty)
            {
                return BadRequest("L'ID du client est requis.");
            }

            try
            {
                var (user, temporaryPassword) = await _authService.CreateOrResetPortalAccessAsync(request.ClientId);
                
                if (user == null || temporaryPassword == null)
                {
                    return StatusCode(500, "Une erreur est survenue lors de la création du compte.");
                }

                return Ok(new 
                {
                    Message = "Accès portail créé/réinitialisé avec succès.",
                    UserId = user.Id,
                    Username = user.NomUtilisateur,
                    TemporaryPassword = temporaryPassword
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                // Log l'exception complète pour le débogage
                Console.WriteLine($"[UsersController] Erreur CreatePortalAccess: {ex}");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }
    }
}