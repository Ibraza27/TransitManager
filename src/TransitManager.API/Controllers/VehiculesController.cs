using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;

namespace TransitManager.API.Controllers
{
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

        // --- GET: api/vehicules ---
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehiculeListItemDto>>> GetVehicules()
        {
            try
            {
                var vehicules = await _vehiculeService.GetAllAsync();
                var vehiculeDtos = vehicules.Select(v => new VehiculeListItemDto
                {
                    Id = v.Id,
                    Immatriculation = v.Immatriculation,
                    Marque = v.Marque,
                    Modele = v.Modele,
                    Statut = v.Statut,
                    ClientNomComplet = v.Client?.NomComplet ?? "N/A",
					ClientTelephonePrincipal = v.Client?.TelephonePrincipal,
                    ConteneurNumeroDossier = v.Conteneur?.NumeroDossier,
					DateCreation = v.DateCreation,
					DestinationFinale = v.DestinationFinale
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
    }
}