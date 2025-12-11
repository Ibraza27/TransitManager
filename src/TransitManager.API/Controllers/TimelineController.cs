using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TimelineController : ControllerBase
    {
        private readonly ITimelineService _timelineService;

        public TimelineController(ITimelineService timelineService)
        {
            _timelineService = timelineService;
        }

        // GET: api/timeline?colisId=...&vehiculeId=...
        [HttpGet]
        public async Task<IActionResult> GetTimeline([FromQuery] Guid? colisId, [FromQuery] Guid? vehiculeId)
        {
            if (!colisId.HasValue && !vehiculeId.HasValue)
                return BadRequest("Un ID de Colis ou de Véhicule est requis.");

            var events = await _timelineService.GetTimelineAsync(colisId, vehiculeId);
            return Ok(events);
        }
    }
}