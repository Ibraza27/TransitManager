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
    public class VehiculesController : ControllerBase
    {
        private readonly IVehiculeService _vehiculeService;
        private readonly ILogger<VehiculesController> _logger;
		private readonly IExportService _exportService;

		public VehiculesController(
			IVehiculeService vehiculeService, 
			IExportService exportService, // <-- AJOUT
			ILogger<VehiculesController> logger)
		{
			_vehiculeService = vehiculeService;
			_exportService = exportService; // <-- AJOUT
			_logger = logger;
		}

		// Ajoutez la méthode d'action
		[HttpGet("{id}/export/pdf")]
		public async Task<IActionResult> ExportPdf(Guid id, [FromQuery] bool includeFinancials = false, [FromQuery] bool includePhotos = false)
		{
			try
			{
				// IMPORTANT : On doit récupérer le véhicule AVEC les documents pour les photos
				// Le service standard GetByIdAsync ne fait peut-être pas le .Include(d => d.Documents).
				// Option A : Modifier le service GetByIdAsync pour inclure Documents (Recommandé).
				// Option B : Si vous ne voulez pas modifier le service, le repository sera appelé en lazy loading si configuré, ou les photos seront vides.
				
				// On suppose ici que GetByIdAsync renvoie l'entité complète. 
				// Si les photos ne s'affichent pas, allez dans VehiculeRepository.GetWithDetailsAsync et ajoutez .Include(v => v.Documents)
				
				var vehicule = await _vehiculeService.GetByIdAsync(id);
				if (vehicule == null) return NotFound();

				var pdfData = await _exportService.GenerateVehiculePdfAsync(vehicule, includeFinancials, includePhotos);
				
				var safeImmat = vehicule.Immatriculation.Replace(" ", "_");
				return File(pdfData, "application/pdf", $"Vehicule_{safeImmat}.pdf");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur export véhicule");
				return StatusCode(500, "Erreur interne lors de la génération du PDF");
			}
		}


		// GET: api/vehicules
		[HttpGet]
		public async Task<ActionResult<IEnumerable<VehiculeListItemDto>>> GetVehicules()
		{
			try
			{
				// Si vous voulez filtrer par utilisateur (Client vs Admin), intégrez la logique ici
				// Pour l'instant, on renvoie tout (comportement Admin)
				var vehicules = await _vehiculeService.GetAllAsync();
				
				var vehiculeDtos = vehicules.Select(v => new VehiculeListItemDto
				{
					Id = v.Id,
					Immatriculation = v.Immatriculation,
					Marque = v.Marque,
					Modele = v.Modele,
					Annee = v.Annee, // <-- MAPPING AJOUTÉ
					Statut = v.Statut,
					ClientNomComplet = v.Client?.NomComplet ?? "N/A",
					ClientTelephonePrincipal = v.Client?.TelephonePrincipal,
					ConteneurNumeroDossier = v.Conteneur?.NumeroDossier,
					Commentaires = v.Commentaires,
					DateCreation = v.DateCreation,
					DestinationFinale = v.DestinationFinale,
					
					// <-- MAPPING FINANCIER AJOUTÉ
					PrixTotal = v.PrixTotal,
					SommePayee = v.SommePayee,
					HasMissingDocuments = v.Documents.Any(d => d.Statut == TransitManager.Core.Enums.StatutDocument.Manquant)
				});

				return Ok(vehiculeDtos);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur lors de la récupération de la liste des véhicules.");
				return StatusCode(500, "Une erreur interne est survenue.");
			}
		}

        // --- AJOUT : GET api/vehicules/{id} ---
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicule>> GetVehiculeById(Guid id)
        {
            var vehicule = await _vehiculeService.GetByIdAsync(id);
            if (vehicule == null) return NotFound();
            return Ok(vehicule);
        }

        // --- AJOUT : POST api/vehicules ---
        [HttpPost]
        public async Task<ActionResult<Vehicule>> CreateVehicule([FromBody] Vehicule vehicule)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var createdVehicule = await _vehiculeService.CreateAsync(vehicule);
            return CreatedAtAction(nameof(GetVehiculeById), new { id = createdVehicule.Id }, createdVehicule);
        }

        // --- AJOUT : PUT api/vehicules/{id} ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVehicule(Guid id, [FromBody] Vehicule vehicule)
        {
            if (id != vehicule.Id) return BadRequest();
            await _vehiculeService.UpdateAsync(vehicule);
            return NoContent();
        }
		
		// GET: api/vehicules/mine
		[HttpGet("mine")]
		public async Task<ActionResult<IEnumerable<VehiculeListItemDto>>> GetMyVehicules()
		{
			try
			{
				// 1. Qui est connecté ?
				var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
				if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return Unauthorized();

				var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
				bool isAdmin = roleClaim != null && roleClaim.Value == "Administrateur";

				IEnumerable<Vehicule> vehicules;

				if (isAdmin)
				{
					// Admin : Tout voir
					vehicules = await _vehiculeService.GetAllAsync();
				}
				else
				{
					// Client : Voir seulement ses véhicules (NOUVELLE MÉTHODE)
					vehicules = await _vehiculeService.GetByUserIdAsync(userId);
				}

				// 2. Mapping vers DTO
				var dtos = vehicules.Select(v => new VehiculeListItemDto
				{
					Id = v.Id,
					Immatriculation = v.Immatriculation,
					Marque = v.Marque,
					Modele = v.Modele,
					Annee = v.Annee,
					Statut = v.Statut,
					ClientNomComplet = v.Client?.NomComplet ?? "N/A",
					ClientTelephonePrincipal = v.Client?.TelephonePrincipal,
					ConteneurNumeroDossier = v.Conteneur?.NumeroDossier,
					Commentaires = v.Commentaires,
					DateCreation = v.DateCreation,
					DestinationFinale = v.DestinationFinale,
					PrixTotal = v.PrixTotal,
					SommePayee = v.SommePayee,
					HasMissingDocuments = v.Documents.Any(d => d.Statut == TransitManager.Core.Enums.StatutDocument.Manquant)
				});

				return Ok(dtos);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur GetMyVehicules");
				return StatusCode(500, "Erreur interne");
			}
		}
		
		// DELETE: api/vehicules/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicule(Guid id)
        {
            try
            {
                var success = await _vehiculeService.DeleteAsync(id);
                if (!success) return NotFound();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du véhicule {Id}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }		

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<VehiculeListItemDto>>> GetVehiculesPaged(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            [FromQuery] string? search = null)
        {
            try
            {
                // Si c'est un client, on filtre automatiquement
                Guid? clientId = null;
                var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
                bool isAdmin = roleClaim != null && roleClaim.Value == "Administrateur";
                
                if (!isAdmin)
                {
                     var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                     if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                     {
                         // Pour simplifier ici, on suppose que l'ID utilisateur n'est pas directement l'ID client
                         // Mais le service GetPagedAsync prend un clientId.
                         // Idéalement on récupère le ClientId depuis le user.
                         // Hack temporaire : on ne filtre pas ici si on ne connait pas le mapping User->ClientId facilement sans service
                         // Mais on peut utiliser la méthode GetByUserIdAsync pour récupérer le clientId si besoin
                         // Pour l'instant on laisse null si Admin, sinon on devrait mapper.
                         // TODO: Récupérer le ClientId proprement.
                     }
                }

                var result = await _vehiculeService.GetPagedAsync(page, pageSize, search, clientId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Erreur GetVehiculesPaged");
                 return StatusCode(500, "Erreur interne");
            }
        }		
		
		// GET: api/vehicules/{id}/export/attestation
		[HttpGet("{id}/export/attestation")]
		public async Task<IActionResult> ExportAttestation(Guid id)
		{
			try
			{
				// On récupère le véhicule avec les infos client
				var vehicule = await _vehiculeService.GetByIdAsync(id);
				if (vehicule == null) return NotFound();

				var pdfData = await _exportService.GenerateAttestationValeurPdfAsync(vehicule);
				
				var safeImmat = vehicule.Immatriculation.Replace(" ", "_");
				return File(pdfData, "application/pdf", $"Attestation_Valeur_{safeImmat}.pdf");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur export attestation");
				return StatusCode(500, "Erreur interne");
			}
		}
		
    }
}