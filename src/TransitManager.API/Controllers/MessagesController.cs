using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TransitManager.Core.DTOs;
using TransitManager.Core.Interfaces;

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
        public async Task<IActionResult> GetMessages([FromQuery] Guid? colisId, [FromQuery] Guid? vehiculeId)
        {
            if (!colisId.HasValue && !vehiculeId.HasValue)
                return BadRequest("Un ID de Colis ou de Véhicule est requis.");

            var userId = GetCurrentUserId();
            var messages = await _messageService.GetMessagesAsync(colisId, vehiculeId, userId);
            
            return Ok(messages);
        }

        // POST: api/messages
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto dto)
        {
            if (!dto.ColisId.HasValue && !dto.VehiculeId.HasValue)
                return BadRequest("Le message doit être lié à un Colis ou un Véhicule.");

            var userId = GetCurrentUserId();
            
            try 
            {
                await _messageService.SendMessageAsync(dto, userId);
                return Ok(new { Message = "Message envoyé" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de l'envoi : {ex.Message}");
            }
        }

        // POST: api/messages/mark-read
        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkAsRead([FromBody] MarkReadDto request)
        {
            var userId = GetCurrentUserId();
            await _messageService.MarkAsReadAsync(request.ColisId, request.VehiculeId, userId);
            return Ok();
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
    }
}