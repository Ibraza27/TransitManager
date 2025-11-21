using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TransitManager.Core.DTOs;
using TransitManager.Infrastructure.Data;
using BCrypt.Net;

namespace TransitManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly TransitContext _context;

        public ProfileController(TransitContext context)
        {
            _context = context;
        }

        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && Guid.TryParse(idClaim.Value, out var id))
            {
                return id;
            }
            throw new UnauthorizedAccessException("Utilisateur non identifié.");
        }

        [HttpGet]
        public async Task<ActionResult<UserProfileDto>> GetMyProfile()
        {
            try
            {
                var userId = GetCurrentUserId();
                
                // On récupère l'utilisateur et son client lié
                var user = await _context.Utilisateurs
                    .Include(u => u.Client)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null) return NotFound("Utilisateur introuvable.");
                if (user.Client == null) return BadRequest("Ce compte n'est pas lié à une fiche client.");

                var dto = new UserProfileDto
                {
                    ClientId = user.Client.Id,
                    Nom = user.Client.Nom,
                    Prenom = user.Client.Prenom,
                    Email = user.Client.Email ?? user.Email,
                    Telephone = user.Client.TelephonePrincipal,
                    TelephoneSecondaire = user.Client.TelephoneSecondaire,
                    Adresse = user.Client.AdressePrincipale,
                    Ville = user.Client.Ville,
                    CodePostal = user.Client.CodePostal,
                    Pays = user.Client.Pays
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur serveur : {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UserProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = GetCurrentUserId();
                var user = await _context.Utilisateurs
                    .Include(u => u.Client)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || user.Client == null) return NotFound("Profil introuvable.");

                // 1. Mise à jour des infos Client
                var client = user.Client;
                client.Nom = dto.Nom;
                client.Prenom = dto.Prenom;
                client.Email = dto.Email;
                client.TelephonePrincipal = dto.Telephone;
                client.TelephoneSecondaire = dto.TelephoneSecondaire;
                client.AdressePrincipale = dto.Adresse;
                client.Ville = dto.Ville;
                client.CodePostal = dto.CodePostal;
                client.Pays = dto.Pays;
                
                // Mise à jour de la date de modification
                client.DateModification = DateTime.UtcNow;
                client.ModifiePar = user.NomUtilisateur;

                // 2. Mise à jour des infos Utilisateur (synchro)
                user.Nom = dto.Nom;
                user.Prenom = dto.Prenom;
                user.Email = dto.Email;
                user.Telephone = dto.Telephone;

                // 3. Gestion du changement de mot de passe
                if (!string.IsNullOrEmpty(dto.AncienMotDePasse) && !string.IsNullOrEmpty(dto.NouveauMotDePasse))
                {
                    // Vérifier l'ancien mot de passe
                    if (!BCrypt.Net.BCrypt.Verify(dto.AncienMotDePasse, user.MotDePasseHash))
                    {
                        return BadRequest("L'ancien mot de passe est incorrect.");
                    }

                    // Hasher le nouveau
                    user.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.NouveauMotDePasse);
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Profil mis à jour avec succès." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la mise à jour : {ex.Message}");
            }
        }
    }
}