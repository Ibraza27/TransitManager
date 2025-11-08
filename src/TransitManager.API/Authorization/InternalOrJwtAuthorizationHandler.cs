using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace TransitManager.API.Authorization
{
    public class InternalOrJwtAuthorizationHandler : AuthorizationHandler<InternalOrJwtRequirement>
    {
        private readonly IConfiguration _configuration;

        public InternalOrJwtAuthorizationHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, InternalOrJwtRequirement requirement)
        {
            // Cas 1: L'utilisateur est déjà authentifié via un token JWT (le client Web)
            if (context.User.Identity?.IsAuthenticated == true)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Cas 2: Vérifier la clé secrète interne (pour les clients WPF/Mobile)
            var httpContext = (context.Resource as HttpContext);
            if (httpContext != null)
            {
                var secretHeader = httpContext.Request.Headers["X-Internal-Secret"].FirstOrDefault();
                var expectedSecret = _configuration["InternalSecret"];

                if (secretHeader != null && secretHeader == expectedSecret)
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }

            // Si aucune des conditions n'est remplie, l'autorisation échoue.
            return Task.CompletedTask;
        }
    }
}