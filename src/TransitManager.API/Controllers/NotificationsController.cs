using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = GetCurrentUserId();
            var notifs = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(notifs);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { Count = count });
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            await _notificationService.MarkAsReadAsync(id);
            return Ok();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok();
        }

        private Guid GetCurrentUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return idClaim != null ? Guid.Parse(idClaim.Value) : Guid.Empty;
        }
    }
}