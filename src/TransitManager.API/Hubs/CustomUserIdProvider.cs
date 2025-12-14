using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TransitManager.API.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // On récupère l'ID de l'utilisateur depuis les Claims (JWT ou Cookie)
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}