using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace TransitManager.Infrastructure.Hubs
{
    public class NotificationHub : Hub
    {
        // Ce hub sert de point de connexion.
        // Les messages sont envoyés depuis le NotificationService via IHubContext.
    }
}