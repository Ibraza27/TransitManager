using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using TransitManager.Infrastructure.Hubs;

namespace TransitManager.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IAuthenticationService _authService;

        public NotificationService(
            IDbContextFactory<TransitContext> contextFactory,
            IHubContext<NotificationHub> hubContext,
            IAuthenticationService authService)
        {
            _contextFactory = contextFactory;
            _hubContext = hubContext;
            _authService = authService;
        }

        public async Task CreateAndSendAsync(
            string title, string message, Guid? userId,
            CategorieNotification categorie, string? actionUrl = null,
            Guid? entityId = null, string? entityType = null,
            PrioriteNotification priorite = PrioriteNotification.Normale)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // 1. Identifier l'expéditeur (celui qui fait l'action)
            var currentUserId = _authService.CurrentUser?.Id;

            // 2. Déterminer les destinataires
            var recipients = new List<Guid>();

            if (userId.HasValue)
            {
                // Cible un utilisateur spécifique
                recipients.Add(userId.Value);
            }
            else
            {
                // Cible tous les Admins/Gestionnaires
                var admins = await context.Utilisateurs
                    .Where(u => u.Role == RoleUtilisateur.Administrateur || u.Role == RoleUtilisateur.Gestionnaire)
                    .Select(u => u.Id)
                    .ToListAsync();
                recipients.AddRange(admins);
            }

            // 3. FILTRE CRITIQUE : Retirer l'utilisateur actuel de la liste des destinataires
            // On ne veut pas s'auto-notifier
            if (currentUserId.HasValue)
            {
                recipients.RemoveAll(id => id == currentUserId.Value);
            }

            // Si plus personne à notifier, on arrête
            if (!recipients.Any()) return;

            // 4. Créer et Sauvegarder
            var notifsToSend = new List<Notification>();

            foreach (var recipientId in recipients)
            {
                var notif = new Notification
                {
                    UtilisateurId = recipientId,
                    Title = title,
                    Message = message,
                    Categorie = categorie,
                    ActionUrl = actionUrl,
                    RelatedEntityId = entityId,
                    RelatedEntityType = entityType,
                    Priorite = priorite,
                    Icone = GetIconForCategory(categorie),
                    Couleur = GetColorForCategory(categorie),
                    DateCreation = DateTime.UtcNow,
                    EstLue = false
                };
                context.Notifications.Add(notif);
                notifsToSend.Add(notif);
            }

            await context.SaveChangesAsync();

            // 5. Envoyer via SignalR
            foreach (var notif in notifsToSend)
            {
                var notifDto = new
                {
                    notif.Id,
                    notif.Title,
                    notif.Message,
                    notif.Icone,
                    notif.Couleur,
                    notif.ActionUrl,
                    notif.DateCreation,
                    notif.Categorie,
                    notif.EstLue
                };

                if (notif.UtilisateurId.HasValue)
                {
                    await _hubContext.Clients.User(notif.UtilisateurId.Value.ToString())
                        .SendAsync("ReceiveNotification", notifDto);
                }
            }
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(Guid userId, int count = 20)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Notifications
                .Where(n => n.UtilisateurId == userId)
                .OrderByDescending(n => n.DateCreation)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Notifications.CountAsync(n => n.UtilisateurId == userId && !n.EstLue);
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var notif = await context.Notifications.FindAsync(notificationId);
            if (notif != null && !notif.EstLue)
            {
                notif.EstLue = true;
                notif.DateLecture = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await context.Notifications
                .Where(n => n.UtilisateurId == userId && !n.EstLue)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.EstLue, true)
                    .SetProperty(n => n.DateLecture, DateTime.UtcNow));
        }

        // Helpers visuels (privés)
        private string GetIconForCategory(CategorieNotification cat) => cat switch
        {
            CategorieNotification.StatutColis => "bi-box-seam",
            CategorieNotification.StatutVehicule => "bi-car-front",
            CategorieNotification.StatutConteneur => "bi-box",
            CategorieNotification.Paiement => "bi-cash-coin",
            CategorieNotification.Document => "bi-file-earmark-text",
            CategorieNotification.Message or CategorieNotification.NouveauMessage => "bi-chat-dots",
            CategorieNotification.Inventaire => "bi-list-check",
            CategorieNotification.NouveauClient => "bi-person-plus",
            CategorieNotification.AlerteDouane => "bi-shield-exclamation",
            _ => "bi-bell"
        };

        private string GetColorForCategory(CategorieNotification cat) => cat switch
        {
            CategorieNotification.Paiement => "text-success",
            CategorieNotification.StatutConteneur => "text-info",
            CategorieNotification.AlerteDouane => "text-danger",
            CategorieNotification.StatutColis or CategorieNotification.StatutVehicule => "text-primary",
            CategorieNotification.Inventaire or CategorieNotification.Document => "text-warning",
            _ => "text-secondary"
        };

        // Implémentation explicite de l'événement de l'interface (même si on ne l'utilise pas ici pour le web)
        public event EventHandler<NotificationEventArgs>? NotificationReceived;

        // Méthode simple NotifyAsync demandée par l'interface héritée (pour compatibilité WPF)
        public async Task NotifyAsync(string title, string message, TypeNotification type = TypeNotification.Information, PrioriteNotification priorite = PrioriteNotification.Normale)
        {
            // On redirige vers la nouvelle méthode plus complète
            await CreateAndSendAsync(title, message, null, CategorieNotification.Systeme, null, null, null, priorite);
        }
    }
}
