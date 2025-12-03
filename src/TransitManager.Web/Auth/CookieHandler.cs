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
			
			// Nom du cookie défini dans l'API et le Web
			var cookieName = "TransitManager.AuthCookie";
			
			// On essaie de récupérer le cookie
			var cookieValue = httpContext?.Request.Cookies[cookieName];

			// Si pas trouvé, on essaie le cookie par défaut AspNetCore (cas de fallback)
			if (string.IsNullOrEmpty(cookieValue))
			{
				cookieValue = httpContext?.Request.Cookies[".AspNetCore.Cookies"];
			}

			if (!string.IsNullOrEmpty(cookieValue))
			{
				// Ajouter le cookie à l'en-tête de la requête sortante vers l'API
				request.Headers.Add("Cookie", $"{cookieName}={cookieValue}");
			}
			
			// OPTIONNEL MAIS RECOMMANDÉ : Ajouter aussi la clé secrète interne comme passe-partout
			// Cela permet de visualiser les images même si le cookie saute, car l'API accepte l'auth hybride
			// (Il faudrait injecter IConfiguration pour récupérer la clé, mais le cookie devrait suffire ici)

			return await base.SendAsync(request, cancellationToken);
		}
    }
}