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
using TransitManager.Core.DTOs;

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
			Guid? relatedEntityId = null, string? relatedEntityType = null,
			PrioriteNotification priorite = PrioriteNotification.Normale)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			// 1. Identifier l'expéditeur (celui qui fait l'action) pour éviter l'auto-notification
			var currentUserId = _authService.CurrentUser?.Id;

			// 2. Déterminer les destinataires
			var recipients = new List<Guid>();

			if (userId.HasValue)
			{
				// CAS 1 : Notification ciblée (ex: pour un Client spécifique)
				recipients.Add(userId.Value);
				
				// IMPORTANT : On ne filtre PAS l'utilisateur courant ici.
				// Si on cible explicitement quelqu'un, il doit recevoir la notif.
			}
			else
			{
				// CAS 2 : Notification Broadcast (pour tous les Admins/Gestionnaires)
				var admins = await context.Utilisateurs
					.Where(u => u.Role == RoleUtilisateur.Administrateur || u.Role == RoleUtilisateur.Gestionnaire)
					.Select(u => u.Id)
					.ToListAsync();
				
				recipients.AddRange(admins);

				// FILTRE : On retire l'utilisateur qui a déclenché l'action s'il fait partie des admins
				// pour éviter qu'il ne reçoive une notif pour sa propre action.
				if (currentUserId.HasValue)
				{
					recipients.RemoveAll(id => id == currentUserId.Value);
				}
			}

			// Si plus personne à notifier, on arrête
			if (!recipients.Any()) return;

			// 3. Créer et Sauvegarder les entités en base
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

					RelatedEntityId = relatedEntityId,
					RelatedEntityType = relatedEntityType,
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

			// 4. Envoyer via SignalR (Temps réel)
			foreach (var notif in notifsToSend)
			{
				// On crée un objet anonyme pour ne pas envoyer tout le graphe d'objets EF
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
					notif.EstLue,
					notif.RelatedEntityId,
					notif.RelatedEntityType
				};

				if (notif.UtilisateurId.HasValue)
				{
					try
					{
						// Envoi ciblé par User ID
						await _hubContext.Clients.User(notif.UtilisateurId.Value.ToString())
							.SendAsync("ReceiveNotification", notifDto);
						
						// Log pour débogage (à retirer en prod si trop verbeux)
						Console.WriteLine($"[NotificationService] SignalR envoyé à {notif.UtilisateurId}");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"[NotificationService] Erreur envoi SignalR : {ex.Message}");
					}
				}
			}
		}

        public async Task CreateAndSendBatchAsync(IEnumerable<NotificationRequest> requests)
        {
            if (requests == null || !requests.Any()) return;

            await using var context = await _contextFactory.CreateDbContextAsync();
            var notifsToSend = new List<Notification>();
            var currentUserId = _authService.CurrentUser?.Id;

            // Pré-chargement des admins si nécessaire
            List<Guid>? adminIds = null;
            if (requests.Any(r => r.UserId == null))
            {
                adminIds = await context.Utilisateurs
                    .Where(u => (u.Role == RoleUtilisateur.Administrateur || u.Role == RoleUtilisateur.Gestionnaire) && u.Actif)
                    .Select(u => u.Id)
                    .ToListAsync();
            }

            foreach (var req in requests)
            {
                var recipients = new List<Guid>();

                if (req.UserId.HasValue)
                {
                    recipients.Add(req.UserId.Value);
                }
                else if (adminIds != null)
                {
                    recipients.AddRange(adminIds);
                    // Filter out current user if admin
                    if (currentUserId.HasValue)
                    {
                        recipients.RemoveAll(id => id == currentUserId.Value);
                    }
                }

                foreach (var recipientId in recipients)
                {
                    var notif = new Notification
                    {
                        UtilisateurId = recipientId,
                        Title = req.Title,
                        Message = req.Message,
                        Categorie = req.Categorie,
                        ActionUrl = req.ActionUrl,
                        RelatedEntityId = req.EntityId,
                        RelatedEntityType = req.EntityType,
                        Priorite = req.Priorite,
                        Icone = GetIconForCategory(req.Categorie),
                        Couleur = GetColorForCategory(req.Categorie),
                        DateCreation = DateTime.UtcNow,
                        EstLue = false
                    };
                    notifsToSend.Add(notif);
                }
            }

            if (!notifsToSend.Any()) return;

            // Insertion par lot en base
            context.Notifications.AddRange(notifsToSend);
            await context.SaveChangesAsync();

            // Envoi SignalR optimisé (Groupé par Utilisateur)
            var groupedNotifs = notifsToSend.GroupBy(n => n.UtilisateurId);

            foreach (var group in groupedNotifs)
            {
                if (!group.Key.HasValue) continue;

                var userIdStr = group.Key.Value.ToString();
                
                foreach (var notif in group)
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
                        notif.EstLue,
                        notif.RelatedEntityId,
                        notif.RelatedEntityType
                    };
                    
                    try 
                    {
                        await _hubContext.Clients.User(userIdStr).SendAsync("ReceiveNotification", notifDto);
                    }
                    catch { /* Ignore SignalR errors in batch */ }
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
