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
    public class PushSubscriptionsController : ControllerBase
    {
        private readonly IWebPushService _webPushService;

        public PushSubscriptionsController(IWebPushService webPushService)
        {
            _webPushService = webPushService;
        }

        /// <summary>
        /// Retourne la clé publique VAPID (nécessaire côté client pour s'abonner)
        /// </summary>
        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            var key = _webPushService.GetVapidPublicKey();
            return Ok(new { publicKey = key });
        }

        /// <summary>
        /// Enregistre un abonnement push pour l'utilisateur connecté
        /// </summary>
        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Endpoint))
                return BadRequest("L'endpoint est requis.");

            var userId = GetCurrentUserId();
            if (userId == Guid.Empty)
                return Unauthorized();

            var userAgent = Request.Headers.UserAgent.ToString();
            await _webPushService.SubscribeAsync(userId, dto.Endpoint, dto.Keys.P256dh, dto.Keys.Auth, userAgent);
            return Ok();
        }

        /// <summary>
        /// Supprime un abonnement push
        /// </summary>
        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Endpoint))
                return BadRequest("L'endpoint est requis.");

            await _webPushService.UnsubscribeAsync(dto.Endpoint);
            return Ok();
        }

        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return idClaim != null ? Guid.Parse(idClaim.Value) : Guid.Empty;
        }
    }

    // DTO inline pour le unsubscribe
    public class PushUnsubscribeDto
    {
        public string Endpoint { get; set; } = string.Empty;
    }
}
