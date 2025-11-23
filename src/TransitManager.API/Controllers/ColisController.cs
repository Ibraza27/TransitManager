using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace TransitManager.API.Controllers
{
	[Authorize]
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
		
        // PUT: api/colis/inventaire
        [HttpPut("inventaire")]
        public async Task<IActionResult> UpdateInventaire([FromBody] UpdateInventaireDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            try
            {
                await _colisService.UpdateInventaireAsync(dto);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'inventaire pour le colis ID {ColisId}", dto.ColisId);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

		// GET: api/colis
		[HttpGet]
		public async Task<ActionResult<IEnumerable<ColisListItemDto>>> GetColis()
		{
			try
			{
				var colisList = await _colisService.GetAllAsync();
				
				var colisDtos = colisList.Select(c => new ColisListItemDto
				{
					Id = c.Id,
					NumeroReference = c.NumeroReference,
					Designation = c.Designation,
					Statut = c.Statut,
					ClientNomComplet = c.Client?.NomComplet ?? "N/A",
					ClientTelephonePrincipal = c.Client?.TelephonePrincipal,
					ConteneurNumeroDossier = c.Conteneur?.NumeroDossier,
					AllBarcodes = string.Join(", ", c.Barcodes.Select(b => b.Value)),
					DestinationFinale = c.DestinationFinale,
					DateArrivee = c.DateArrivee,
					
					// --- AJOUTS CRUCIAUX ---
					NombrePieces = c.NombrePieces,
					PrixTotal = c.PrixTotal,
					SommePayee = c.SommePayee
					// -----------------------
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
        public async Task<ActionResult<Colis>> CreateColis([FromBody] CreateColisDto colisDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            try
            {
                // On passe directement le DTO au service
                var createdColis = await _colisService.CreateAsync(colisDto);
                return CreatedAtAction(nameof(GetColisById), new { id = createdColis.Id }, createdColis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du colis.");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateColis(Guid id, [FromBody] UpdateColisDto colisDto)
        {
            if (id != colisDto.Id) return BadRequest("Incohérence des IDs.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            
            try
            {
                // On passe l'ID et le DTO au service
                await _colisService.UpdateAsync(id, colisDto);
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
		
		// GET: api/colis/mine
		[HttpGet("mine")]
		public async Task<ActionResult<IEnumerable<ColisListItemDto>>> GetMyColis()
		{
			try
			{
				// 1. Récupérer l'ID de l'utilisateur
				var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
				if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
				{
					return Unauthorized();
				}

				// 2. Vérifier le rôle (Admin ou pas ?)
				var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
				bool isAdmin = roleClaim != null && roleClaim.Value == "Administrateur";

				IEnumerable<Colis> colisList;

				if (isAdmin)
				{
					// Si Admin : On récupère TOUT via la méthode existante GetAllAsync
					colisList = await _colisService.GetAllAsync();
				}
				else
				{
					// Si Client : On récupère seulement les siens via la méthode qu'on a créée
					colisList = await _colisService.GetByUserIdAsync(userId);
				}

				// 3. Mapping vers le DTO (identique pour les deux cas)
				var colisDtos = colisList.Select(c => new ColisListItemDto
				{
					Id = c.Id,
					NumeroReference = c.NumeroReference,
					Designation = c.Designation,
					Statut = c.Statut,
					ClientNomComplet = c.Client?.NomComplet ?? "N/A",
					ClientTelephonePrincipal = c.Client?.TelephonePrincipal,
					ConteneurNumeroDossier = c.Conteneur?.NumeroDossier,
					AllBarcodes = string.Join(", ", c.Barcodes.Select(b => b.Value)),
					DestinationFinale = c.DestinationFinale,
					DateArrivee = c.DateArrivee,
					NombrePieces = c.NombrePieces,
					PrixTotal = c.PrixTotal,
					SommePayee = c.SommePayee
				});

				return Ok(colisDtos);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur lors de la récupération des colis.");
				return StatusCode(500, "Une erreur interne est survenue.");
			}
		}
		
    }
}