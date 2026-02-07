using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using WebPush;

namespace TransitManager.Infrastructure.Services
{
    public class WebPushService : IWebPushService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly IConfiguration _configuration;
        private readonly WebPushClient _webPushClient;

        public WebPushService(IDbContextFactory<TransitContext> contextFactory, IConfiguration configuration)
        {
            _contextFactory = contextFactory;
            _configuration = configuration;
            _webPushClient = new WebPushClient();
        }

        public async Task SendNotificationAsync(string endpoint, string p256dh, string auth, string title, string message, string? actionUrl = null)
        {
            var subject = _configuration["VapidSettings:Subject"];
            var publicKey = _configuration["VapidSettings:PublicKey"];
            var privateKey = _configuration["VapidSettings:PrivateKey"];

            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(publicKey) || string.IsNullOrEmpty(privateKey))
            {
                Console.WriteLine("[WebPushService] Configuring VAPID keys is required in appsettings.json");
                return;
            }

            var subscription = new PushSubscription(endpoint, p256dh, auth);
            var vapidDetails = new VapidDetails(subject, publicKey, privateKey);

            var payload = new
            {
                title = title,
                body = message,
                icon = "images/icons/icon-192x192.png",
                badge = "images/icons/icon-72x72.png",
                data = new { url = actionUrl ?? "/" }
            };

            try
            {
                await _webPushClient.SendNotificationAsync(subscription, JsonSerializer.Serialize(payload), vapidDetails);
            }
            catch (WebPushException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Gone) 
                {
                    // Subscription is invalid, should be removed
                     await RemoveSubscriptionAsync(endpoint);
                }
                else
                {
                    Console.WriteLine($"[WebPushService] Error sending push: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebPushService] General error: {ex.Message}");
            }
        }

        public async Task SendToUserAsync(Guid userId, string title, string message, string? actionUrl = null)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var subscriptions = await context.PushSubscriptions
                .Where(s => s.UtilisateurId == userId)
                .ToListAsync();

            foreach (var sub in subscriptions)
            {
                await SendNotificationAsync(sub.Endpoint, sub.P256dh ?? "", sub.Auth ?? "", title, message, actionUrl);
            }
        }
        
        public async Task SendToUsersAsync(List<Guid> userIds, string title, string message, string? actionUrl = null)
        {
             using var context = await _contextFactory.CreateDbContextAsync();
            var subscriptions = await context.PushSubscriptions
                .Where(s => s.UtilisateurId.HasValue && userIds.Contains(s.UtilisateurId.Value))
                .ToListAsync();

            foreach (var sub in subscriptions)
            {
                 await SendNotificationAsync(sub.Endpoint, sub.P256dh ?? "", sub.Auth ?? "", title, message, actionUrl);
            }
        }
        
        private async Task RemoveSubscriptionAsync(string endpoint)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                var sub = await context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == endpoint);
                if (sub != null)
                {
                    context.PushSubscriptions.Remove(sub);
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebPushService] Error removing subscription: {ex.Message}");
            }
        }

        public Task CleanupSubscriptionsAsync()
        {
            // Optional: Implement periodic cleanup logic if needed
            return Task.CompletedTask;
        }
    }
}
