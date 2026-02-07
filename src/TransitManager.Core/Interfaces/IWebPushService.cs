using System;
using System.Threading.Tasks;

namespace TransitManager.Core.Interfaces
{
    public interface IWebPushService
    {
        /// <summary>
        /// Enregistre ou met à jour un abonnement push pour un utilisateur
        /// </summary>
        Task SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth, string? userAgent = null);

        /// <summary>
        /// Supprime un abonnement push par endpoint
        /// </summary>
        Task UnsubscribeAsync(string endpoint);

        /// <summary>
        /// Envoie une notification push à tous les appareils d'un utilisateur
        /// </summary>
        Task SendToUserAsync(Guid userId, string title, string message, string? url = null, string? icon = null);

        /// <summary>
        /// Envoie une notification push à plusieurs utilisateurs
        /// </summary>
        Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string message, string? url = null, string? icon = null);

        /// <summary>
        /// Retourne la clé publique VAPID (nécessaire côté client pour s'abonner)
        /// </summary>
        string GetVapidPublicKey();
    }
}
