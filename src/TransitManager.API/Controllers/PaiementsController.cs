using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace TransitManager.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class PaiementsController : ControllerBase
    {
        private readonly IPaiementService _paiementService;
        private readonly ILogger<PaiementsController> _logger;

        public PaiementsController(IPaiementService paiementService, ILogger<PaiementsController> logger)
        {
            _paiementService = paiementService;
            _logger = logger;
        }
		
        [HttpGet("colis/{colisId}")]
        public async Task<ActionResult<IEnumerable<Paiement>>> GetPaiementsForColis(Guid colisId)
        {
            try
            {
                var paiements = await _paiementService.GetByColisAsync(colisId);
                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements pour le colis ID {ColisId}", colisId);
                return StatusCode(500, "Erreur interne");
            }
        }

        // GET: api/paiements/vehicule/{vehiculeId}
        [HttpGet("vehicule/{vehiculeId}")]
        public async Task<ActionResult<IEnumerable<Paiement>>> GetPaiementsForVehicule(Guid vehiculeId)
        {
            try
            {
                var paiements = await _paiementService.GetByVehiculeAsync(vehiculeId);
                return Ok(paiements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des paiements pour le véhicule ID {VehiculeId}", vehiculeId);
                return StatusCode(500, "Erreur interne");
            }
        }

        // POST: api/paiements
        [HttpPost]
        public async Task<ActionResult<Paiement>> CreatePaiement([FromBody] Paiement paiement)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var createdPaiement = await _paiementService.CreateAsync(paiement);
                return CreatedAtAction(nameof(GetPaiementById), new { id = createdPaiement.Id }, createdPaiement);
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Erreur lors de la création du paiement.");
                return StatusCode(500, "Erreur interne");
            }
        }
        
        // GET: api/paiements/{id} (utile pour le CreatedAtAction)
        [HttpGet("{id}")]
        public async Task<ActionResult<Paiement>> GetPaiementById(Guid id)
        {
            var paiement = await _paiementService.GetByIdAsync(id);
            if (paiement == null) return NotFound();
            return Ok(paiement);
        }

        // DELETE: api/paiements/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePaiement(Guid id)
        {
            try
            {
                var success = await _paiementService.DeleteAsync(id);
                if (!success) return NotFound();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du paiement ID {PaiementId}", id);
                return StatusCode(500, "Erreur interne");
            }
        }
		
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePaiement(Guid id, [FromBody] Paiement paiement)
        {
            if (id != paiement.Id)
            {
                return BadRequest("L'ID de l'URL ne correspond pas à l'ID du paiement.");
            }

            try
            {
                await _paiementService.UpdateAsync(paiement);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du paiement ID {PaiementId}", id);
                return StatusCode(500, "Erreur interne");
            }
        }
        [HttpGet("client/{clientId}/balance")]
        public async Task<ActionResult<decimal>> GetClientBalance(Guid clientId)
        {
            try
            {
                var balance = await _paiementService.GetClientBalanceAsync(clientId);
                return Ok(balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur balance client {ClientId}", clientId);
                return StatusCode(500, "Erreur interne");
            }
        }
    }
}