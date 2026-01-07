using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitManager.Infrastructure.Services;
using System.Threading.Tasks;

namespace TransitManager.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly ISettingsService _settingsService;

        public SettingsController(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<string>> Get(string key)
        {
            var value = await _settingsService.GetSettingAsync(key);
            return Ok(new { value }); // Return object to be consistent or just string?
            // ApiService usually expects JSON object or primitive. Let's return object { "value": "..." }
        }
    }
}
