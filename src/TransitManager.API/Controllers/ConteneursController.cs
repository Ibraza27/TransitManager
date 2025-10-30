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
        private readonly ILogger<ConteneursController> _logger;

        public ConteneursController(IConteneurService conteneurService, ILogger<ConteneursController> logger)
        {
            _conteneurService = conteneurService;
            _logger = logger;
        }

        // GET: api/conteneurs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Conteneur>>> GetConteneurs()
        {
            try
            {
                var conteneurs = await _conteneurService.GetAllAsync();
                return Ok(conteneurs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la liste des conteneurs.");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // --- DÉBUT DES AJOUTS ---

        // GET: api/conteneurs/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Conteneur>> GetConteneurById(Guid id)
        {
            try
            {
                var conteneur = await _conteneurService.GetByIdAsync(id);
                if (conteneur == null)
                {
                    return NotFound();
                }
                return Ok(conteneur);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du conteneur ID {ConteneurId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // POST: api/conteneurs
        [HttpPost]
        public async Task<ActionResult<Conteneur>> CreateConteneur([FromBody] Conteneur conteneur)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdConteneur = await _conteneurService.CreateAsync(conteneur);
                return CreatedAtAction(nameof(GetConteneurById), new { id = createdConteneur.Id }, createdConteneur);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du conteneur.");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // PUT: api/conteneurs/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConteneur(Guid id, [FromBody] Conteneur conteneur)
        {
            if (id != conteneur.Id)
            {
                return BadRequest("L'ID de l'URL ne correspond pas à l'ID du conteneur.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _conteneurService.UpdateAsync(conteneur);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du conteneur ID {ConteneurId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }
        // --- FIN DES AJOUTS ---
    }
}