using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Core.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace TransitManager.API.Controllers
{
	[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(IClientService clientService, ILogger<ClientsController> logger)
        {
            _clientService = clientService;
            _logger = logger;
        }

        // --- GET: api/clients ---
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Client>>> GetClients()
        {
            try
            {
                // --- DÉBUT DE LA MODIFICATION ---
                var clients = await _clientService.GetActiveClientsAsync();
                return Ok(clients); // On renvoie la liste complète directement
                // --- FIN DE LA MODIFICATION ---
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la liste des clients.");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // --- GET: api/clients/{id} ---
        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClient(Guid id)
        {
            try
            {
                var client = await _clientService.GetByIdAsync(id);
                if (client == null) return NotFound();
                return Ok(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du client ID {ClientId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // --- AJOUT : POST api/clients ---
        [HttpPost]
        public async Task<ActionResult<Client>> CreateClient([FromBody] Client client)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var createdClient = await _clientService.CreateAsync(client);
                // Retourne le client créé avec un code 201 Created et l'URL pour y accéder
                return CreatedAtAction(nameof(GetClient), new { id = createdClient.Id }, createdClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du client.");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // --- AJOUT : PUT api/clients/{id} ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClient(Guid id, [FromBody] Client client)
        {
            if (id != client.Id)
            {
                return BadRequest("L'ID de l'URL ne correspond pas à l'ID du client.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _clientService.UpdateAsync(client);
                // Retourne 204 No Content, la norme pour un PUT réussi sans renvoi de données
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du client ID {ClientId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }
        
        // --- AJOUT : DELETE api/clients/{id} ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(Guid id)
        {
            try
            {
                var success = await _clientService.DeleteAsync(id);
                if (!success)
                {
                    return NotFound();
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du client ID {ClientId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<Client>>> GetClientsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            try
            {
                var result = await _clientService.GetPagedAsync(page, pageSize, search);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération paginée des clients.");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }
    }
}