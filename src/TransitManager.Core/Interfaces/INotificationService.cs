using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAsync(string title, string message, TypeNotification type = TypeNotification.Information, PrioriteNotification priorite = PrioriteNotification.Normale);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId);
        Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(Guid userId);
        Task<int> GetUnreadCountAsync();
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
        Task DeleteNotificationAsync(Guid notificationId);
        event EventHandler<NotificationEventArgs>? NotificationReceived;
    }
}