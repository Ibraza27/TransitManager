using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TransitManager.Core.DTOs;
using TransitManager.Core.Interfaces;
using TransitManager.Core.Entities;

namespace TransitManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        // GET: api/messages?colisId=...&vehiculeId=...
		[HttpGet]
        public async Task<IActionResult> GetMessages([FromQuery] Guid? colisId, [FromQuery] Guid? vehiculeId, [FromQuery] Guid? conteneurId)
        {
            // Vérifiez que cette ligne inclut bien conteneurId.HasValue
            if (!colisId.HasValue && !vehiculeId.HasValue && !conteneurId.HasValue)
                return BadRequest("Un ID de Colis, Véhicule ou Conteneur est requis.");

            var userId = GetCurrentUserId();
            var messages = await _messageService.GetMessagesAsync(colisId, vehiculeId, conteneurId, userId);
            
            return Ok(messages);
        }

        // POST: api/messages
		[HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto dto)
        {
            // On ajoute la vérification pour le Conteneur
            if (!dto.ColisId.HasValue && !dto.VehiculeId.HasValue && !dto.ConteneurId.HasValue)
                return BadRequest("Le message doit être lié à un Colis, un Véhicule ou un Conteneur.");

            var userId = GetCurrentUserId();
            
            try 
            {
                var param = await _messageService.SendMessageAsync(dto, userId);
                return Ok(new { Message = "Message envoyé", Id = param.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de l'envoi : {ex.Message}");
            }
        }

        // POST: api/messages/mark-read
        // POST: api/messages/mark-read
        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkAsRead([FromQuery] Guid? colisId, [FromQuery] Guid? vehiculeId, [FromQuery] Guid? conteneurId)
        {
            var userId = GetCurrentUserId();
            await _messageService.MarkAsReadAsync(colisId, vehiculeId, conteneurId, userId);
            return Ok();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrateur,SuperAdmin,Gestionnaire")]
        public async Task<IActionResult> DeleteMessage(Guid id)
        {
            await _messageService.DeleteMessageAsync(id);
            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim != null && Guid.TryParse(idClaim.Value, out var id))
            {
                return id;
            }
            // Fallback pour dev ou cas particuliers, mais ne devrait pas arriver avec [Authorize]
            return Guid.Empty;
        }
    }

    // Petit DTO local pour la requête mark-read
    public class MarkReadDto
    {
        public Guid? ColisId { get; set; }
        public Guid? VehiculeId { get; set; }
        public Guid? ConteneurId { get; set; } // AJOUT
    }
}