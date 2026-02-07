using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Infrastructure.Data;

namespace TransitManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PushSubscriptionsController : ControllerBase
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly IConfiguration _configuration;

        public PushSubscriptionsController(IDbContextFactory<TransitContext> contextFactory, IConfiguration configuration)
        {
            _contextFactory = contextFactory;
            _configuration = configuration;
        }

        [HttpGet("vapid-public-key")]
        public IActionResult GetVapidPublicKey()
        {
            var publicKey = _configuration["VapidSettings:PublicKey"];
            if (string.IsNullOrEmpty(publicKey))
                return NotFound("VAPID public key not configured.");
            
            return Ok(new { publicKey });
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] PushSubscriptionDto subscriptionDto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            await using var context = await _contextFactory.CreateDbContextAsync();

            // Check if subscription already exists
            var existing = await context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == subscriptionDto.Endpoint);

            if (existing != null)
            {
                // Update properties if changed
                existing.P256dh = subscriptionDto.P256dh;
                existing.Auth = subscriptionDto.Auth;
                existing.UtilisateurId = userId; // Ensure it's linked to current user
                existing.UserAgent = Request.Headers["User-Agent"].ToString();
            }
            else
            {
                var newSub = new PushSubscription
                {
                    Endpoint = subscriptionDto.Endpoint,
                    P256dh = subscriptionDto.P256dh,
                    Auth = subscriptionDto.Auth,
                    UtilisateurId = userId,
                    UserAgent = Request.Headers["User-Agent"].ToString()
                };
                context.PushSubscriptions.Add(newSub);
            }

            await context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] JsonElement payload)
        {
             string? endpoint = null;
             if (payload.TryGetProperty("Endpoint", out var endpointProp) || payload.TryGetProperty("endpoint", out endpointProp))
             {
                 endpoint = endpointProp.GetString();
             }

             if (string.IsNullOrEmpty(endpoint)) return BadRequest("Endpoint required");

            await using var context = await _contextFactory.CreateDbContextAsync();
            var sub = await context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
            
            if (sub != null)
            {
                context.PushSubscriptions.Remove(sub);
                await context.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
