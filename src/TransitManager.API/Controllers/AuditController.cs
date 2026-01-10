using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [Authorize(Roles = "Administrateur")]
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;

        public AuditController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            [FromQuery] string? userId = null, 
            [FromQuery] string? entityName = null, 
            [FromQuery] DateTime? date = null)
        {
            var result = await _auditService.GetAuditLogsAsync(page, pageSize, userId, entityName, date);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuditLogById(Guid id)
        {
            var log = await _auditService.GetAuditLogByIdAsync(id);
            if (log == null)
            {
                return NotFound();
            }
            return Ok(log);
        }
    }
}
