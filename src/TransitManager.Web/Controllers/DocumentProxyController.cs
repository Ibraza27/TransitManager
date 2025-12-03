using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace TransitManager.Web.Controllers
{
    [Authorize] // Sécurise l'accès côté Web
    [Route("api/documents")] // Mappe l'URL générée par votre bouton
    public class DocumentProxyController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;

        public DocumentProxyController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        [HttpGet("{id}/preview")]
        public async Task<IActionResult> Preview(Guid id)
        {
            // 1. On récupère le client HTTP "API" configuré dans Program.cs
            // Il contient déjà l'URL de base de l'API et le Handler pour les cookies/auth
            var client = _clientFactory.CreateClient("API");

            // 2. On appelle l'API réelle
            var response = await client.GetAsync($"api/documents/{id}/preview");

            if (!response.IsSuccessStatusCode)
            {
                return NotFound("Document introuvable ou accès refusé sur l'API.");
            }

            // 3. On récupère le flux et le type de contenu (PDF, Image...)
            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

            // 4. On renvoie le fichier au navigateur pour affichage (Inline)
            // Cela permet au PDF de s'ouvrir dans l'onglet au lieu de se télécharger
            Response.Headers.Append("Content-Disposition", "inline");
            
            return File(stream, contentType);
        }
    }
}