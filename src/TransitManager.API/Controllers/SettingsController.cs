using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransitManager.Infrastructure.Services;
using TransitManager.Core.Interfaces;
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
        [HttpPost("upload-logo")]
        public async Task<IActionResult> UploadLogo([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier reçu.");

            try 
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                // Unique filename
                var ext = Path.GetExtension(file.FileName);
                var fileName = $"logo_{DateTime.Now.Ticks}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return URL relative to web root
                var url = $"/images/{fileName}";
                return Ok(url);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur interne: {ex.Message}");
            }
        }
    }
}
