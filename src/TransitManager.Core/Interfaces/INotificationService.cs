using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.DTOs;

namespace TransitManager.Core.Interfaces
{
    public interface INotificationService
    {
        // Méthode générique puissante (Nouvelle)
        Task CreateAndSendAsync(
            string title, 
            string message, 
            Guid? userId, // Null = Pour les Admins
            CategorieNotification categorie,
            string? actionUrl = null,
            Guid? entityId = null,
            string? entityType = null,

            PrioriteNotification priorite = PrioriteNotification.Normale);

        // Méthode de traitement par lot (Batch Processing)
        Task CreateAndSendBatchAsync(IEnumerable<NotificationRequest> requests);

        Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, int count = 20);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
        
        // --- MÉTHODE DE COMPATIBILITÉ (POUR CORRIGER L'ERREUR) ---
        Task NotifyAsync(string title, string message, TypeNotification type = TypeNotification.Information, PrioriteNotification priorite = PrioriteNotification.Normale);
        
        // Événement pour le WPF (optionnel si tu utilises SignalR partout, mais gardons-le pour la sécurité)
        event EventHandler<NotificationEventArgs>? NotificationReceived;
    }
}