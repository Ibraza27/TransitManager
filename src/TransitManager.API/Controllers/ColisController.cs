using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TransitManager.API.Controllers
{
	[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ColisController : ControllerBase
    {
        private readonly IColisService _colisService;
        private readonly ILogger<ColisController> _logger;
		private readonly IExportService _exportService;

		// Modifiez le constructeur
		public ColisController(
			IColisService colisService, 
			IExportService exportService, // <-- AJOUT
			ILogger<ColisController> logger)
		{
			_colisService = colisService;
			_exportService = exportService; // <-- AJOUT
			_logger = logger;
		}


		// GET: api/colis/{id}/export/pdf?includeFinancials=true&includePhotos=true
        [HttpGet("{id}/export/pdf")]
        public async Task<IActionResult> ExportPdf(Guid id, [FromQuery] bool includeFinancials = false, [FromQuery] bool includePhotos = false)
        {
            try
            {
                // Le service GetByIdAsync inclut maintenant les Documents
                var colis = await _colisService.GetByIdAsync(id);
                if (colis == null) return NotFound();

                // On passe le nouveau paramètre
                var pdfData = await _exportService.GenerateColisPdfAsync(colis, includeFinancials, includePhotos);
                
                var safeName = colis.NumeroReference.Replace("/", "-");
                return File(pdfData, "application/pdf", $"Colis_{safeName}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur export colis");
                return StatusCode(500, "Erreur lors de la génération du PDF");
            }
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
					FraisDouane = c.FraisDouane, // AJOUT
					TypeEnvoi = c.TypeEnvoi, // AJOUT
					SommePayee = c.SommePayee,
					HasMissingDocuments = c.Documents.Any(d => d.Statut == TransitManager.Core.Enums.StatutDocument.Manquant),
					IsExcludedFromExport = c.IsExcludedFromExport
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
        [HttpGet("{id:guid}")]
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
        
        [HttpPost("{id}/toggle-export")]
        public async Task<IActionResult> ToggleExportExclusion(Guid id, [FromBody] bool isExcluded)
        {
            try
            {
                var success = await _colisService.SetExportExclusionAsync(id, isExcluded);
                if (!success) return NotFound();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement d'exclusion export pour {ColisId}", id);
                return StatusCode(500, "Erreur interne");
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
					FraisDouane = c.FraisDouane, // ADJUST
					TypeEnvoi = c.TypeEnvoi, // ADJUST
					SommePayee = c.SommePayee,
					HasMissingDocuments = c.Documents.Any(d => d.Statut == TransitManager.Core.Enums.StatutDocument.Manquant),
					IsExcludedFromExport = c.IsExcludedFromExport
				});

				return Ok(colisDtos);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur lors de la récupération des colis.");
				return StatusCode(500, "Une erreur interne est survenue.");
			}
		}
		
		
		[HttpGet("{id}/export/ticket")]
        public async Task<IActionResult> ExportTicket(Guid id, [FromQuery] string format = "thermal")
        {
            try
            {
                var colis = await _colisService.GetByIdAsync(id);
                if (colis == null) return NotFound();

                // Appel de la méthode existante du service d'export (celle utilisée par WPF)
                var pdfData = await _exportService.GenerateColisTicketPdfAsync(colis, format);
                
                var safeRef = colis.NumeroReference.Replace("/", "-");
                return File(pdfData, "application/pdf", $"Ticket_{safeRef}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur export ticket");
                return StatusCode(500, "Erreur interne");
            }
        }
		
		[HttpGet("paged")]
		public async Task<ActionResult<PagedResult<ColisListItemDto>>> GetColisPaged(
			[FromQuery] int page = 1, 
			[FromQuery] int pageSize = 20, 
			[FromQuery] string? search = null)
		{
			// Si c'est un client, on filtre automatiquement
			Guid? clientId = null;
			var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
			if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
			{
				 // Récupérer le ClientId de l'utilisateur si ce n'est pas un admin
				 // (Logique à adapter selon votre gestion des rôles)
			}

			var result = await _colisService.GetPagedAsync(page, pageSize, search, clientId);
			return Ok(result);
		}
		
    }
}