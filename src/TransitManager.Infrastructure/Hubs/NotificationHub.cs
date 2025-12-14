using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace TransitManager.Infrastructure.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.Identity?.Name ?? "Anonyme";
            
            Console.WriteLine($"🔔 [HUB] Connexion établie : User={userName} ({userId}), ID connexion={Context.ConnectionId}");
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userName = Context.User?.Identity?.Name ?? "Anonyme";
            
            if (exception != null)
            {
                Console.WriteLine($"🔕 [HUB] Déconnexion ERREUR pour {userName} : {exception.Message}");
            }
            else
            {
                Console.WriteLine($"🔕 [HUB] Déconnexion normale pour {userName}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}