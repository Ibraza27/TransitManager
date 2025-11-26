using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [Authorize(Roles = "Administrateur")] // Sécurité : Seul l'admin peut gérer les utilisateurs
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthenticationService _authService;

        public UsersController(IUserService userService, IAuthenticationService authService)
        {
            _userService = userService;
            _authService = authService;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Utilisateur>>> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Utilisateur>> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        // POST: api/users
        public class CreateUserRequest
        {
            public Utilisateur User { get; set; } = new();
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost]
        public async Task<ActionResult<Utilisateur>> Create([FromBody] CreateUserRequest request)
        {
            try
            {
                var createdUser = await _userService.CreateAsync(request.User, request.Password);
                return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Utilisateur user)
        {
            if (id != user.Id) return BadRequest();
            try
            {
                await _userService.UpdateAsync(user);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _userService.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        // POST: api/users/create-portal-access (Méthode existante conservée)
        public class CreatePortalAccessRequest { public Guid ClientId { get; set; } }

        [HttpPost("create-portal-access")]
        public async Task<IActionResult> CreatePortalAccess([FromBody] CreatePortalAccessRequest request)
        {
            if (request.ClientId == Guid.Empty) return BadRequest("L'ID du client est requis.");
            try
            {
                var (user, temporaryPassword) = await _authService.CreateOrResetPortalAccessAsync(request.ClientId);
                if (user == null || temporaryPassword == null) return StatusCode(500, "Erreur interne.");
                
                return Ok(new { Message = "Accès créé.", UserId = user.Id, Username = user.NomUtilisateur, TemporaryPassword = temporaryPassword });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
		
		// Dans UsersController.cs
		[HttpPost("{id}/reset-password")]
		public async Task<ActionResult<string>> ResetPassword(Guid id)
		{
			var newPass = await _userService.ResetPasswordAsync(id);
			if (newPass == null) return NotFound();
			return Ok(newPass);
		}
		
		[HttpPost("{id}/unlock")]
		public async Task<IActionResult> UnlockAccount(Guid id)
		{
			var success = await _userService.UnlockAccountAsync(id);
			if (!success) return NotFound();
			return Ok(new { Message = "Compte déverrouillé avec succès." });
		}

		public class ChangePasswordRequest { public string NewPassword { get; set; } = ""; }

		[HttpPost("{id}/change-password")]
		public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.NewPassword)) return BadRequest("Le mot de passe est requis.");
			var success = await _userService.ChangePasswordManualAsync(id, request.NewPassword);
			if (!success) return NotFound();
			return Ok(new { Message = "Mot de passe modifié." });
		}
    }
}