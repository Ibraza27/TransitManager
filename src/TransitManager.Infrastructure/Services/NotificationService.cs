using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly IAuthenticationService _authenticationService;
        public event EventHandler<NotificationEventArgs>? NotificationReceived;

        public NotificationService(IDbContextFactory<TransitContext> contextFactory, IAuthenticationService authenticationService)
        {
            _contextFactory = contextFactory;
            _authenticationService = authenticationService;
        }

        public async Task NotifyAsync(string title, string message, TypeNotification type = TypeNotification.Information, PrioriteNotification priorite = PrioriteNotification.Normale)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Priorite = priorite,
                // Vérification de l'utilisateur actuel avant assignation
                UtilisateurId = _authenticationService.CurrentUser?.Id != Guid.Empty
                    ? _authenticationService.CurrentUser?.Id
                    : null
            };
            context.Set<Notification>().Add(notification);
            await context.SaveChangesAsync();
            NotificationReceived?.Invoke(this, new NotificationEventArgs
            {
                Title = title,
                Message = message,
                Type = type,
                Priorite = priorite
            });
            if (priorite == PrioriteNotification.Haute || priorite == PrioriteNotification.Urgente)
            {
                await SendEmailNotificationAsync(notification);
                if (priorite == PrioriteNotification.Urgente)
                {
                    await SendSmsNotificationAsync(notification);
                }
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Notification>()
                .Where(n => n.UtilisateurId == userId || n.UtilisateurId == null)
                .OrderByDescending(n => n.DateCreation)
                .Take(100)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Set<Notification>()
                .Where(n => (n.UtilisateurId == userId || n.UtilisateurId == null) && !n.EstLue)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var userId = _authenticationService.CurrentUser?.Id;
            if (!userId.HasValue) return 0;
            return await context.Set<Notification>()
                .CountAsync(n => (n.UtilisateurId == userId || n.UtilisateurId == null) && !n.EstLue);
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var notification = await context.Set<Notification>().FindAsync(notificationId);
            if (notification != null)
            {
                notification.EstLue = true;
                notification.DateLecture = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var notifications = await context.Set<Notification>()
                .Where(n => (n.UtilisateurId == userId || n.UtilisateurId == null) && !n.EstLue)
                .ToListAsync();
            foreach (var notification in notifications)
            {
                notification.EstLue = true;
                notification.DateLecture = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(Guid notificationId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var notification = await context.Set<Notification>().FindAsync(notificationId);
            if (notification != null)
            {
                context.Set<Notification>().Remove(notification);
                await context.SaveChangesAsync();
            }
        }

        private Task SendEmailNotificationAsync(Notification notification)
        {
            // TODO: Implémenter l'envoi d'email
            return Task.CompletedTask;
        }

        private Task SendSmsNotificationAsync(Notification notification)
        {
            // TODO: Implémenter l'envoi de SMS
            return Task.CompletedTask;
        }
    }
}
