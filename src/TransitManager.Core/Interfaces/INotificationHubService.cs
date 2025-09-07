using System;
using System.Threading.Tasks;

namespace TransitManager.Core.Interfaces
{
    public interface INotificationHubService
    {
        Task NotifyClientUpdated(Guid clientId);
        // Ajoutez ici d'autres méthodes de notification si besoin (Colis, Vehicule...)
    }
}