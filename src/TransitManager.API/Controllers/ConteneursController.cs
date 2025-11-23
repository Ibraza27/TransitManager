using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace TransitManager.API.Controllers
{
	[Authorize]
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
		
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteConteneur(Guid id)
        {
            try
            {
                var success = await _conteneurService.DeleteAsync(id);
                if (!success)
                {
                    return NotFound();
                }
                return NoContent(); // Code 204 : Succès, pas de contenu à retourner
            }
            catch (InvalidOperationException ex)
            {
                // Si la règle métier (conteneur non vide) est violée, on renvoie une erreur 400
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du conteneur ID {ConteneurId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }
		
		// GET: api/conteneurs/mine
		[HttpGet("mine")]
		public async Task<ActionResult<IEnumerable<Conteneur>>> GetMyConteneurs()
		{
			try
			{
				var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
				if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) 
					return Unauthorized();

				var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
				bool isAdmin = roleClaim != null && roleClaim.Value == "Administrateur";

				if (isAdmin)
				{
					// Admin : Tout voir
					return Ok(await _conteneurService.GetAllAsync());
				}
				else
				{
					// Client : Voir seulement les siens
					// On récupère d'abord l'ID du client lié au user
					// Note: Idéalement injecter IUserService, mais on peut le faire via le contexte si nécessaire
					// Ici on suppose que le service Conteneur ne gère pas les users.
					// Utilisons une méthode simple : User -> ClientId via User Service ou Claims si ajouté.
					
					// Si vous avez ajouté le Claim "client_id" lors du login (ce qu'on a fait précédemment) :
					var clientIdClaim = User.FindFirst("client_id");
					if (clientIdClaim != null && Guid.TryParse(clientIdClaim.Value, out var clientId))
					{
						return Ok(await _conteneurService.GetByClientIdAsync(clientId));
					}
					
					return Ok(new List<Conteneur>()); // Pas de client lié = pas de conteneur
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erreur GetMyConteneurs");
				return StatusCode(500, "Erreur interne");
			}
		}
		
    }
}