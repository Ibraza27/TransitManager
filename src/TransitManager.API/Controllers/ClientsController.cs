using Microsoft.AspNetCore.Mvc;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Core.DTOs;

namespace TransitManager.API.Controllers
{
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

        // GET: api/clients
        [HttpGet]
        // MODIFICATION : La méthode retourne maintenant une liste de DTOs
        public async Task<ActionResult<IEnumerable<ClientListItemDto>>> GetClients()
        {
            try
            {
                var clients = await _clientService.GetActiveClientsAsync();

                // Mappage de l'entité vers le DTO
                var clientDtos = clients.Select(c => new ClientListItemDto
                {
                    Id = c.Id,
                    CodeClient = c.CodeClient,
                    NomComplet = c.NomComplet,
                    TelephonePrincipal = c.TelephonePrincipal,
                    Email = c.Email
                });

                return Ok(clientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la liste des clients.");
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // GET: api/clients/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Client>> GetClient(Guid id)
        {
            try
            {
                var client = await _clientService.GetByIdAsync(id);
                if (client == null)
                {
                    return NotFound();
                }
                return Ok(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du client ID {ClientId}", id);
                return StatusCode(500, "Une erreur interne est survenue.");
            }
        }

        // Vous ajouterez ici les autres méthodes (POST pour créer, PUT pour modifier, etc.)
        // au fur et à mesure que vous développerez l'application mobile.
    }
}