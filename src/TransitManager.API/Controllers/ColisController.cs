using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ColisController : ControllerBase
    {
        private readonly IColisService _colisService;
        private readonly ILogger<ColisController> _logger;

        public ColisController(IColisService colisService, ILogger<ColisController> logger)
        {
            _colisService = colisService;
            _logger = logger;
        }

        // --- GET: api/colis ---
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ColisListItemDto>>> GetColis()
        {
            try
            {
                var colisList = await _colisService.GetAllAsync(); // Cette méthode charge déjà les barcodes
                
                var colisDtos = colisList.Select(c => new ColisListItemDto
                {
                    Id = c.Id,
                    NumeroReference = c.NumeroReference,
                    Designation = c.Designation,
                    Statut = c.Statut,
                    ClientNomComplet = c.Client?.NomComplet ?? "N/A",
                    ConteneurNumeroDossier = c.Conteneur?.NumeroDossier,
                    // --- AJOUTER CETTE LIGNE ---
                    AllBarcodes = string.Join(", ", c.Barcodes.Select(b => b.Value))
                });
                
                return Ok(colisDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la liste des colis.");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // --- AJOUT : GET api/colis/{id} ---
        [HttpGet("{id}")]
        public async Task<ActionResult<Colis>> GetColisById(Guid id)
        {
            try
            {
                var colis = await _colisService.GetByIdAsync(id);
                if (colis == null) return NotFound();
                return Ok(colis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du colis ID {ColisId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // --- AJOUT : POST api/colis ---
        [HttpPost]
        public async Task<ActionResult<Colis>> CreateColis([FromBody] Colis colis)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            // --- CORRECTION API ---
            // On établit manuellement la relation inverse avant de passer au service.
            foreach (var barcode in colis.Barcodes)
            {
                barcode.Colis = colis;
            }
            // --- FIN CORRECTION ---

            try
            {
                var createdColis = await _colisService.CreateAsync(colis);
                return CreatedAtAction(nameof(GetColisById), new { id = createdColis.Id }, createdColis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du colis.");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateColis(Guid id, [FromBody] Colis colis)
        {
            if (id != colis.Id) return BadRequest("L'ID de l'URL ne correspond pas à l'ID du colis.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            // --- CORRECTION API ---
            foreach (var barcode in colis.Barcodes)
            {
                barcode.Colis = colis;
            }
            // --- FIN CORRECTION ---

            try
            {
                await _colisService.UpdateAsync(colis);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du colis ID {ColisId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // --- AJOUT : DELETE api/colis/{id} ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteColis(Guid id)
        {
            try
            {
                var success = await _colisService.DeleteAsync(id);
                if (!success) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du colis ID {ColisId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }
    }
}