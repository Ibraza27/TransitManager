// src/TransitManager.API/Authorization/HybridAuthorizationHandler.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace TransitManager.API.Authorization
{
    public class HybridAuthorizationHandler : AuthorizationHandler<HybridRequirement>
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HybridAuthorizationHandler(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HybridRequirement requirement)
        {
            // --- CAS 1: L'utilisateur est DÉJÀ authentifié (via Cookie ou JWT) ---
            // Le middleware d'authentification a déjà fait le travail.
            if (context.User.Identity?.IsAuthenticated == true)
            {
                Console.WriteLine("🔑 [HybridAuth] SUCCÈS - Autorisation via identité existante (Cookie ou JWT).");
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // --- CAS 2: L'utilisateur n'est PAS authentifié, on vérifie la clé secrète (pour Mobile/WPF) ---
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var secretHeader = httpContext.Request.Headers["X-Internal-Secret"].FirstOrDefault();
                var expectedSecret = _configuration["InternalSecret"];

                if (!string.IsNullOrEmpty(secretHeader) && secretHeader == expectedSecret)
                {
                    Console.WriteLine("🔑 [HybridAuth] SUCCÈS - Autorisation via en-tête de clé secrète interne.");
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            // --- ÉCHEC ---
            // Si aucune des conditions n'est remplie, l'autorisation échoue implicitement.
            Console.WriteLine("🔑 [HybridAuth] ÉCHEC - Aucune identité valide ou clé secrète trouvée.");
            return Task.CompletedTask;
        }
    }
}