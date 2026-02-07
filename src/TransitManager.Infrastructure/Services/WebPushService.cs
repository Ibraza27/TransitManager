using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using WebPush;

namespace TransitManager.Infrastructure.Services
{
    public class WebPushService : IWebPushService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly ILogger<WebPushService> _logger;
        private readonly VapidDetails _vapidDetails;
        private readonly WebPushClient _pushClient;

        public WebPushService(
            IDbContextFactory<TransitContext> contextFactory,
            IConfiguration configuration,
            ILogger<WebPushService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;

            var vapidSubject = configuration["VapidSettings:Subject"] ?? "mailto:admin@transitmanager.com";
            var vapidPublicKey = configuration["VapidSettings:PublicKey"] ?? throw new InvalidOperationException("VapidSettings:PublicKey non configuré dans appsettings.json");
            var vapidPrivateKey = configuration["VapidSettings:PrivateKey"] ?? throw new InvalidOperationException("VapidSettings:PrivateKey non configuré dans appsettings.json");

            _vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
            _pushClient = new WebPushClient();
        }

        public string GetVapidPublicKey() => _vapidDetails.PublicKey;

        public async Task SubscribeAsync(Guid userId, string endpoint, string p256dh, string auth, string? userAgent = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var existing = await context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint);

            if (existing != null)
            {
                existing.UtilisateurId = userId;
                existing.P256dh = p256dh;
                existing.Auth = auth;
                existing.UserAgent = userAgent;
            }
            else
            {
                context.PushSubscriptions.Add(new Core.Entities.PushSubscription
                {
                    UtilisateurId = userId,
                    Endpoint = endpoint,
                    P256dh = p256dh,
                    Auth = auth,
                    UserAgent = userAgent
                });
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("[WebPush] Abonnement enregistré pour l'utilisateur {UserId}", userId);
        }

        public async Task UnsubscribeAsync(string endpoint)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var subscription = await context.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == endpoint);

            if (subscription != null)
            {
                context.PushSubscriptions.Remove(subscription);
                await context.SaveChangesAsync();
                _logger.LogInformation("[WebPush] Abonnement supprimé pour endpoint {Endpoint}", endpoint[..Math.Min(50, endpoint.Length)]);
            }
        }

        public async Task SendToUserAsync(Guid userId, string title, string message, string? url = null, string? icon = null)
        {
            await SendToUsersAsync(new[] { userId }, title, message, url, icon);
        }

        public async Task SendToUsersAsync(IEnumerable<Guid> userIds, string title, string message, string? url = null, string? icon = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var userIdList = userIds.ToList();
            var subscriptions = await context.PushSubscriptions
                .Where(s => userIdList.Contains(s.UtilisateurId))
                .ToListAsync();

            if (!subscriptions.Any())
            {
                _logger.LogDebug("[WebPush] Aucun abonnement trouvé pour les utilisateurs : {UserIds}", string.Join(", ", userIdList));
                return;
            }

            var payload = JsonSerializer.Serialize(new
            {
                title,
                body = message,
                icon = icon ?? "/images/logo.jpg",
                badge = "/images/logo.jpg",
                url = url ?? "/",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            var expiredEndpoints = new List<string>();

            foreach (var sub in subscriptions)
            {
                try
                {
                    var pushSubscription = new WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                    await _pushClient.SendNotificationAsync(pushSubscription, payload, _vapidDetails);
                }
                catch (WebPushException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Gone || ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("[WebPush] Abonnement expiré, suppression : {Endpoint}", sub.Endpoint[..Math.Min(50, sub.Endpoint.Length)]);
                    expiredEndpoints.Add(sub.Endpoint);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[WebPush] Erreur lors de l'envoi push");
                }
            }

            if (expiredEndpoints.Any())
            {
                var toRemove = await context.PushSubscriptions
                    .Where(s => expiredEndpoints.Contains(s.Endpoint))
                    .ToListAsync();
                context.PushSubscriptions.RemoveRange(toRemove);
                await context.SaveChangesAsync();
                _logger.LogInformation("[WebPush] {Count} abonnement(s) expiré(s) supprimé(s)", toRemove.Count);
            }
        }
    }
}
