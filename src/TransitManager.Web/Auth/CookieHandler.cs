// src/TransitManager.Web/Auth/CookieHandler.cs

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TransitManager.Web.Auth
{
    public class CookieHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            // Tentative de récupération du cookie d'authentification actuel
            var cookie = httpContext?.Request.Cookies[".AspNetCore.Cookies"];
            
            // NOTE : Le nom du cookie par défaut est ".AspNetCore.Cookies".
            // Nous utilisons celui-ci car c'est le projet Web qui le gère.
            // Notre nom "TransitManager.AuthCookie" était pour l'API.
            var cookieName = "TransitManager.AuthCookie";
            cookie = httpContext?.Request.Cookies[cookieName];

            if (cookie != null)
            {
                // Ajouter le cookie à l'en-tête de la requête sortante vers l'API
                request.Headers.Add("Cookie", $"{cookieName}={cookie}");
                Console.WriteLine($"🍪 [CookieHandler] Cookie '{cookieName}' ajouté à la requête sortante vers l'API.");
            }
            else
            {
                Console.WriteLine($"🍪 [CookieHandler] ⚠️ Aucun cookie '{cookieName}' trouvé à transférer.");
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}