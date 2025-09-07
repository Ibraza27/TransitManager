using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace TransitManager.API.Hubs
{
    public class NotificationHub : Hub
    {
        // Cette méthode est appelée par le NotificationHubService de la couche Infrastructure.
        // Son rôle est de prendre le message et de le renvoyer à TOUS les autres clients.
        public async Task ClientUpdated(Guid clientId)
        {
            // "Clients.All" envoie à tout le monde.
            // "Clients.Others" envoie à tout le monde SAUF à l'expéditeur. C'est souvent mieux.
            // "ClientUpdated" est le nom de l'événement que les clients WPF écouteront.
            await Clients.Others.SendAsync("ClientUpdated", clientId);
        }

        // Ajoutez ici d'autres méthodes pour les autres entités
        // public async Task ColisUpdated(Guid colisId) { ... }
    }
}