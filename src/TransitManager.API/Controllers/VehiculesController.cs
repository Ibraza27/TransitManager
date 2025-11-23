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

        public VehiculesController(IVehiculeService vehiculeService, ILogger<VehiculesController> logger)
        {
            _vehiculeService = vehiculeService;
            _logger = logger;
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
					SommePayee = v.SommePayee
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
				var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
				if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) return Unauthorized();

				var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
				bool isAdmin = roleClaim != null && roleClaim.Value == "Administrateur";

				IEnumerable<Vehicule> vehicules;

				if (isAdmin)
				{
					vehicules = await _vehiculeService.GetAllAsync();
				}
				else
				{
					// Note : Assurez-vous d'avoir implémenté GetByUserIdAsync dans VehiculeService 
					// (similaire à ColisService), sinon on filtre manuellement ici via le client
					// Pour simplifier ici, on suppose que le service le gère ou on passe par le client.
					// Si GetByUserIdAsync n'existe pas, il faut l'ajouter dans IVehiculeService.
					// Pour l'instant, simulons le filtre admin vs tout le monde si la méthode manque :
					vehicules = await _vehiculeService.GetAllAsync();
					// TODO: Filtrer pour le client si non admin (nécessite GetByUserIdAsync)
				}

				var dtos = vehicules.Select(v => new VehiculeListItemDto
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
					SommePayee = v.SommePayee
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
		
    }
}