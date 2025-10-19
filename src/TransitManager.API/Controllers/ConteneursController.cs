using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConteneursController : ControllerBase
    {
        private readonly IConteneurService _conteneurService;

        public ConteneursController(IConteneurService conteneurService)
        {
            _conteneurService = conteneurService;
        }

        // GET: api/conteneurs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Conteneur>>> GetConteneurs()
        {
            var conteneurs = await _conteneurService.GetAllAsync();
            return Ok(conteneurs);
        }
    }
}