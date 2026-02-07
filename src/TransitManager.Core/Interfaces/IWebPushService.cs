using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TransitManager.Core.Interfaces
{
    public interface IWebPushService
    {
        Task SendNotificationAsync(string endpoint, string p256dh, string auth, string title, string message, string? actionUrl = null);
        Task SendToUserAsync(Guid userId, string title, string message, string? actionUrl = null);
        Task SendToUsersAsync(List<Guid> userIds, string title, string message, string? actionUrl = null);
    }
}
