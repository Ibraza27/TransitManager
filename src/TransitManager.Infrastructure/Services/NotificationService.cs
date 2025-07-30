using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services // <-- Le bon namespace
{
    public class NotificationService : INotificationService
    {
        private readonly TransitContext _context;
        private readonly IAuthenticationService _authenticationService;

        public event EventHandler<NotificationEventArgs>? NotificationReceived;

        public NotificationService(TransitContext context, IAuthenticationService authenticationService)
        {
            _context = context;
            _authenticationService = authenticationService;
        }

        public async Task NotifyAsync(string title, string message, TypeNotification type = TypeNotification.Information, PrioriteNotification priorite = PrioriteNotification.Normale)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                Priorite = priorite,
                //DateCreation est gérée par BaseEntity, on la retire d'ici
                UtilisateurId = _authenticationService.CurrentUser?.Id
            };

            _context.Set<Notification>().Add(notification);
            await _context.SaveChangesAsync();

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
            return await _context.Set<Notification>()
                .Where(n => n.UtilisateurId == userId || n.UtilisateurId == null)
                .OrderByDescending(n => n.DateCreation)
                .Take(100)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(Guid userId)
        {
            return await _context.Set<Notification>()
                .Where(n => (n.UtilisateurId == userId || n.UtilisateurId == null) && !n.EstLue)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            var userId = _authenticationService.CurrentUser?.Id;
            if (!userId.HasValue) return 0;

            return await _context.Set<Notification>()
                .CountAsync(n => (n.UtilisateurId == userId || n.UtilisateurId == null) && !n.EstLue);
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _context.Set<Notification>().FindAsync(notificationId);
            if (notification != null)
            {
                notification.EstLue = true;
                notification.DateLecture = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await _context.Set<Notification>()
                .Where(n => (n.UtilisateurId == userId || n.UtilisateurId == null) && !n.EstLue)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.EstLue = true;
                notification.DateLecture = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(Guid notificationId)
        {
            var notification = await _context.Set<Notification>().FindAsync(notificationId);
            if (notification != null)
            {
                _context.Set<Notification>().Remove(notification);
                await _context.SaveChangesAsync();
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