using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using TransitManager.Core.DTOs;

namespace TransitManager.API.Controllers
{
	[Authorize]
    [ApiController]
    [Route("api/[controller]")]
	public class ConteneursController : ControllerBase
    {
        private readonly IConteneurService _conteneurService;
        private readonly IColisService _colisService;
        private readonly IVehiculeService _vehiculeService;
        private readonly IExportService _exportService; // <--- 1. AJOUTER CE CHAMP
        private readonly ILogger<ConteneursController> _logger;

        public ConteneursController(
            IConteneurService conteneurService, 
            IColisService colisService,
            IVehiculeService vehiculeService,
            IExportService exportService,
            ILogger<ConteneursController> logger)
        {
            _conteneurService = conteneurService;
            _colisService = colisService;
            _vehiculeService = vehiculeService;
            _exportService = exportService;
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
		
		// GET: api/conteneurs/{id}/detail
        [HttpGet("{id}/detail")]
        public async Task<ActionResult<ConteneurDetailDto>> GetConteneurDetail(Guid id)
        {
            var conteneur = await _conteneurService.GetByIdAsync(id);
            if (conteneur == null) return NotFound();

            // Mapping manuel vers le DTO (ou AutoMapper si configuré)
            var dto = new ConteneurDetailDto
            {
                Id = conteneur.Id,
                NumeroDossier = conteneur.NumeroDossier,
                NumeroPlomb = conteneur.NumeroPlomb,
                NomCompagnie = conteneur.NomCompagnie,
                NomTransitaire = conteneur.NomTransitaire,
                Destination = conteneur.Destination,
                PaysDestination = conteneur.PaysDestination,
                Statut = conteneur.Statut,
                Commentaires = conteneur.Commentaires,
                DateReception = conteneur.DateReception,
                DateChargement = conteneur.DateChargement,
                DateDepart = conteneur.DateDepart,
                DateArriveeDestination = conteneur.DateArriveeDestination,
                DateDedouanement = conteneur.DateDedouanement,
                DateCloture = conteneur.DateCloture,
                MissingDocumentsCount = conteneur.Documents.Count(d => d.Statut == Core.Enums.StatutDocument.Manquant)
            };

            // Remplissage des listes et calculs
            foreach (var c in conteneur.Colis)
            {
                dto.Colis.Add(new ColisListItemDto
                {
                    Id = c.Id,
                    NumeroReference = c.NumeroReference,
                    Designation = c.Designation,
                    Statut = c.Statut,
                    ClientNomComplet = c.Client?.NomComplet ?? "N/A",
                    ClientTelephonePrincipal = c.Client?.TelephonePrincipal,
                    ConteneurNumeroDossier = conteneur.NumeroDossier,
                    AllBarcodes = string.Join(", ", c.Barcodes.Select(b => b.Value)),
                    DestinationFinale = c.DestinationFinale,
                    DateArrivee = c.DateArrivee,
                    NombrePieces = c.NombrePieces,
                    PrixTotal = c.PrixTotal,
                    SommePayee = c.SommePayee,
                    IsExcludedFromExport = c.IsExcludedFromExport
                });
            }

            foreach (var v in conteneur.Vehicules)
            {
                dto.Vehicules.Add(new VehiculeListItemDto
                {
                    Id = v.Id,
                    Immatriculation = v.Immatriculation,
                    Marque = v.Marque,
                    Modele = v.Modele,
                    Annee = v.Annee,
                    Statut = v.Statut,
                    ClientNomComplet = v.Client?.NomComplet ?? "N/A",
                    ClientTelephonePrincipal = v.Client?.TelephonePrincipal,
                    ConteneurNumeroDossier = conteneur.NumeroDossier,
                    Commentaires = v.Commentaires,
                    DateCreation = v.DateCreation,
                    DestinationFinale = v.DestinationFinale,
                    PrixTotal = v.PrixTotal,
                    SommePayee = v.SommePayee
                });
            }

            // Calculs Globaux
            dto.PrixTotalGlobal = dto.Colis.Sum(c => c.PrixTotal) + dto.Vehicules.Sum(v => v.PrixTotal);
            dto.TotalPayeGlobal = dto.Colis.Sum(c => c.SommePayee) + dto.Vehicules.Sum(v => v.SommePayee);

            // Calculs par Client
            var clients = conteneur.Colis.Select(c => c.Client)
                .Union(conteneur.Vehicules.Select(v => v.Client))
                .Where(c => c != null)
                .DistinctBy(c => c!.Id);

            foreach (var client in clients)
            {
                var colisClient = dto.Colis.Where(c => c.ClientNomComplet == client!.NomComplet).ToList();
                var vehiculesClient = dto.Vehicules.Where(v => v.ClientNomComplet == client!.NomComplet).ToList();

                dto.StatsParClient.Add(new ClientConteneurStatDto
                {
                    ClientId = client!.Id,
                    NomClient = client.NomComplet,
					Telephone = client.TelephonePrincipal,
                    NombreColis = colisClient.Count,
                    NombreVehicules = vehiculesClient.Count,
                    TotalPrix = colisClient.Sum(x => x.PrixTotal) + vehiculesClient.Sum(x => x.PrixTotal),
                    TotalPaye = colisClient.Sum(x => x.SommePayee) + vehiculesClient.Sum(x => x.SommePayee)
                });
            }

            return Ok(dto);
        }

        // POST: api/conteneurs/{id}/colis/assign
		[HttpPost("{id}/colis/assign")]
		public async Task<IActionResult> AssignColis(Guid id, [FromBody] List<Guid> colisIds)
		{
			foreach (var colisId in colisIds)
			{
				await _colisService.AssignToConteneurAsync(colisId, id);
			}
			return Ok();
		}

		[HttpPost("{id}/colis/unassign")]
		public async Task<IActionResult> UnassignColis(Guid id, [FromBody] List<Guid> colisIds)
		{
			foreach (var colisId in colisIds)
			{
				await _colisService.RemoveFromConteneurAsync(colisId);
			}
			return Ok();
		}

		[HttpPost("{id}/vehicules/assign")]
		public async Task<IActionResult> AssignVehicules(Guid id, [FromBody] List<Guid> vehiculeIds)
		{
			foreach (var vId in vehiculeIds)
			{
				await _vehiculeService.AssignToConteneurAsync(vId, id);
			}
			return Ok();
		}

		[HttpPost("{id}/vehicules/unassign")]
		public async Task<IActionResult> UnassignVehicules(Guid id, [FromBody] List<Guid> vehiculeIds)
		{
			foreach (var vId in vehiculeIds)
			{
				await _vehiculeService.RemoveFromConteneurAsync(vId);
			}
			return Ok();
		}
		
		// GET: api/conteneurs/{id}/export/pdf?includeFinancials=true
		[HttpGet("{id}/export/pdf")] 
		public async Task<IActionResult> ExportPdf(Guid id, [FromQuery] bool includeFinancials = false)
		{
			Console.WriteLine($"Step 4: [API CONTROLLER] Requête reçue pour ID: {id}");

			try 
			{
				// Vérification préalable
				var conteneur = await _conteneurService.GetByIdAsync(id);
				if (conteneur == null) 
				{
					Console.WriteLine("Step 4b: [API CONTROLLER] Conteneur introuvable en BDD.");
					return NotFound("Conteneur introuvable");
				}

				Console.WriteLine($"Step 4c: [API CONTROLLER] Conteneur trouvé : {conteneur.NumeroDossier}. Appel du service Export...");

				// Appel du service
				var pdfData = await _exportService.GenerateContainerPdfAsync(conteneur, includeFinancials);

				Console.WriteLine($"Step 5: [API CONTROLLER] PDF généré. Taille : {pdfData.Length}");
				
				// Nettoyage du nom pour le header HTTP
				var safeName = conteneur.NumeroDossier.Replace(" ", "_").Replace("/", "-");
				return File(pdfData, "application/pdf", $"Dossier_{safeName}.pdf");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Step ERROR [API CONTROLLER]: {ex.Message}");
				Console.WriteLine(ex.StackTrace);
				return StatusCode(500, $"Erreur interne API : {ex.Message}");
			}
		}
		
    }
}