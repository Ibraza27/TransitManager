using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR; // AJOUT
using TransitManager.Infrastructure.Hubs;

namespace TransitManager.Infrastructure.Services
{
    public class MessageService : IMessageService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly INotificationService _notificationService;
		private readonly IHubContext<AppHub> _hubContext;

        public MessageService(IDbContextFactory<TransitContext> contextFactory, INotificationService notificationService, IHubContext<AppHub> hubContext)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
			_hubContext = hubContext;
        }


		public async Task<Message> SendMessageAsync(CreateMessageDto dto, Guid senderId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			var sender = await context.Utilisateurs.FindAsync(senderId);
			if (sender == null) throw new Exception("Expéditeur inconnu");

			string? attachmentName = null;
			if (dto.DocumentId.HasValue)
			{
				var doc = await context.Documents.FindAsync(dto.DocumentId.Value);
				if (doc != null) attachmentName = doc.NomFichierOriginal;
			}

			var message = new Message
			{
				Contenu = dto.Contenu,
				ExpediteurId = senderId,
				ColisId = dto.ColisId,
				VehiculeId = dto.VehiculeId,
				ConteneurId = dto.ConteneurId,
				IsInternal = dto.IsInternal,
				DocumentId = dto.DocumentId,
				DateEnvoi = DateTime.UtcNow
			};

			context.Messages.Add(message);
			await context.SaveChangesAsync();
            
            Console.WriteLine($"[MessageService] Message créé. ID: {message.Id}, HasRelatedEntityId: {true}");

			// SignalR
			var messageDto = new MessageDto
			{
				Id = message.Id,
				Contenu = message.Contenu,
				DateEnvoi = message.DateEnvoi,
				NomExpediteur = sender.NomComplet,
				IsInternal = message.IsInternal,
				IsRead = message.IsRead,
				EstMoi = false,
				EstAdmin = sender.Role == RoleUtilisateur.Administrateur,
				HasAttachment = dto.DocumentId.HasValue,
				AttachmentId = dto.DocumentId,
				AttachmentName = attachmentName
			};

			string groupName = dto.ColisId.HasValue ? dto.ColisId.ToString()
								 : dto.VehiculeId.HasValue ? dto.VehiculeId.ToString()
								 : dto.ConteneurId.ToString() ?? "General";
			
			if (!string.IsNullOrEmpty(groupName))
			{
				await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);
			}

			// Notifications
			if (sender.Role == RoleUtilisateur.Client)
			{
				// Le Client écrit -> Notifier les admins
                Console.WriteLine($"[MessageService] Notifying Admins for Message {message.Id}");
				await _notificationService.CreateAndSendAsync(
					"Nouveau Message",
					$"Message de {sender.NomComplet} : {Truncate(dto.Contenu, 30)}", // Utilisation de notre helper
					null, // Admin
					CategorieNotification.NouveauMessage,
					actionUrl: GetMessageUrl(dto),
                    relatedEntityId: message.Id, // AJOUT CORRECTIF
                    relatedEntityType: "Message"
				);
			}
			else if (sender.Role == RoleUtilisateur.Administrateur || sender.Role == RoleUtilisateur.Gestionnaire)
            {
                // Un Admin écrit...

                // 1. Si ce n'est pas une note interne -> Notifier le client concerné
                if (!dto.IsInternal)
                {
				    Guid? clientId = null;
				    if (dto.ColisId.HasValue)
				    {
					    var colis = await context.Colis.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == dto.ColisId);
					    clientId = colis?.ClientId;
				    }
				    else if (dto.VehiculeId.HasValue)
				    {
					    var vehicule = await context.Vehicules.Include(v => v.Client).FirstOrDefaultAsync(v => v.Id == dto.VehiculeId);
					    clientId = vehicule?.ClientId;
				    }
                    // Conteneur = Pas de client unique

                    if (clientId.HasValue)
				    {
					    var clientUser = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == clientId);
					    if (clientUser != null)
					    {
                            Console.WriteLine($"[MessageService] Notifying Client {clientUser.Id} for Message {message.Id}");
						    await _notificationService.CreateAndSendAsync(
							    "Nouveau Message",
							    $"Vous avez reçu un message : {Truncate(dto.Contenu, 30)}",
							    clientUser.Id,
							    CategorieNotification.NouveauMessage,
							    actionUrl: GetMessageUrl(dto),
                                relatedEntityId: message.Id, 
                                relatedEntityType: "Message"
						    );
					    }
				    }
                }

                // 2. ET DANS TOUS LES CAS -> Notifier les AUTRES Admins (Broadcast)
                Console.WriteLine($"[MessageService] Broadcasting Admin Notification for Message {message.Id}");
                await _notificationService.CreateAndSendAsync(
                    "Nouveau Message Admin",
                    $"{sender.NomComplet} : {Truncate(dto.Contenu, 30)}",
                    null, // Broadcast Admin (exclut l'expéditeur automatiquement)
                    CategorieNotification.NouveauMessage,
                    actionUrl: GetMessageUrl(dto),
                    relatedEntityId: message.Id,
                    relatedEntityType: "Message"
                );
            }

			return message;
		}

		// --- AJOUTER CETTE MÉTHODE PRIVÉE EN BAS DE LA CLASSE ---
		private string Truncate(string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value)) return value;
			return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
		}

		private string GetMessageUrl(CreateMessageDto dto)
		{
			if (dto.ColisId.HasValue)
				return $"/colis/edit/{dto.ColisId}?tab=discussion";
			else if (dto.VehiculeId.HasValue)
				return $"/vehicule/edit/{dto.VehiculeId}?tab=discussion";
			else if (dto.ConteneurId.HasValue)
				return $"/conteneur/detail/{dto.ConteneurId}?tab=discussion";
			else
				return "#";
		}


        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid? colisId, Guid? vehiculeId, Guid? conteneurId, Guid currentUserId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var currentUser = await context.Utilisateurs.FindAsync(currentUserId);
            bool isAdmin = currentUser?.Role == RoleUtilisateur.Administrateur || currentUser?.Role == RoleUtilisateur.Gestionnaire;

            var query = context.Messages
                .Include(m => m.Expediteur)
                .Include(m => m.PieceJointe)
                .AsNoTracking();

			if (colisId.HasValue) query = query.Where(m => m.ColisId == colisId);
			else if (vehiculeId.HasValue) query = query.Where(m => m.VehiculeId == vehiculeId);
			else if (conteneurId.HasValue) query = query.Where(m => m.ConteneurId == conteneurId); // AJOUT
			else return new List<MessageDto>();

            // Filtrer les notes internes si l'utilisateur n'est pas admin
            if (!isAdmin)
            {
                query = query.Where(m => !m.IsInternal);
            }

            var messages = await query.OrderBy(m => m.DateEnvoi).ToListAsync();

            return messages.Select(m => new MessageDto
            {
                Id = m.Id,
                Contenu = m.Contenu,
                DateEnvoi = m.DateEnvoi,
                NomExpediteur = m.Expediteur.NomComplet, // Assurez-vous que NomComplet est dispo ou utilisez Prenom + Nom
                EstMoi = m.ExpediteurId == currentUserId,
                EstAdmin = m.Expediteur.Role == RoleUtilisateur.Administrateur || m.Expediteur.Role == RoleUtilisateur.Gestionnaire,
                IsInternal = m.IsInternal,
                IsRead = m.IsRead,
                HasAttachment = m.DocumentId.HasValue,
                AttachmentName = m.PieceJointe?.NomFichierOriginal,
                AttachmentId = m.DocumentId
            });
        }

        public async Task MarkAsReadAsync(Guid? colisId, Guid? vehiculeId, Guid? conteneurId, Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Marquer comme lus les messages qui ne viennent PAS de moi
            var query = context.Messages.Where(m => m.ExpediteurId != userId && !m.IsRead);

            if (colisId.HasValue) query = query.Where(m => m.ColisId == colisId);
            else if (vehiculeId.HasValue) query = query.Where(m => m.VehiculeId == vehiculeId);
			else if (conteneurId.HasValue) query = query.Where(m => m.ConteneurId == conteneurId);

            var unreadMessages = await query.ToListAsync();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                    msg.DateLecture = DateTime.UtcNow;
                }
                await context.SaveChangesAsync();
            }

        }

        public async Task DeleteMessageAsync(Guid messageId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var message = await context.Messages.FindAsync(messageId);
            if (message != null)
            {
                // Identification du groupe pour SignalR
                string groupName = message.ColisId.HasValue ? message.ColisId.ToString()
                                 : message.VehiculeId.HasValue ? message.VehiculeId.ToString()
                                 : message.ConteneurId.ToString() ?? "General";

                context.Messages.Remove(message);
                await context.SaveChangesAsync();

                // 1. Notifier les clients via SignalR (mise à jour du Chat)
                if (!string.IsNullOrEmpty(groupName))
                {
                    await _hubContext.Clients.Group(groupName).SendAsync("MessageDeleted", messageId);
                }

                // 2. Supprimer la notification associée (pour que l'utilisateur ne clique pas sur une notif invalide)
                await _notificationService.DeleteByEntityAsync(messageId, CategorieNotification.NouveauMessage);
            }
        }
    }
}