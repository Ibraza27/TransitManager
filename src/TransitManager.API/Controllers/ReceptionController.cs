using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.Entities;
using TransitManager.Infrastructure.Services;
using TransitManager.Core.DTOs;

namespace TransitManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceptionController : ControllerBase
    {
        private readonly IReceptionService _receptionService;

        public ReceptionController(IReceptionService receptionService)
        {
            _receptionService = receptionService;
        }

        [HttpPost]
        public async Task<ActionResult<ReceptionControl>> Create(ReceptionControl control)
        {
            var created = await _receptionService.CreateControlAsync(control);
            return Ok(created);
        }

        [HttpGet("entity/{type}/{id}")]
        public async Task<ActionResult<ReceptionControl>> GetByEntity(string type, Guid id)
        {
            var control = await _receptionService.GetByEntityAsync(type, id);
            return Ok(control);
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Administrateur")]
        public async Task<ActionResult<ReceptionStatsDto>> GetStats([FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null)
        {
            return Ok(await _receptionService.GetStatsAsync(start, end));
        }
        
        [HttpGet("recent")]
        [Authorize(Roles = "Administrateur")]
        public async Task<ActionResult<List<ReceptionControl>>> GetRecent([FromQuery] int count = 20)
        {
            return Ok(await _receptionService.GetRecentControlsAsync(count));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrateur")]
        public async Task<ActionResult> Delete(Guid id)
        {
            var success = await _receptionService.DeleteControlAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
