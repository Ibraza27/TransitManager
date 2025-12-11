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

            // --- CORRECTION : On récupère les infos du document SI il y en a un ---
            string? attachmentName = null;
            if (dto.DocumentId.HasValue)
            {
                var doc = await context.Documents.FindAsync(dto.DocumentId.Value);
                if (doc != null) attachmentName = doc.NomFichierOriginal;
            }
            // ----------------------------------------------------------------------

            var message = new Message
            {
                Contenu = dto.Contenu,
                ExpediteurId = senderId,
                ColisId = dto.ColisId,
                VehiculeId = dto.VehiculeId,
                IsInternal = dto.IsInternal,
                DocumentId = dto.DocumentId,
                DateEnvoi = DateTime.UtcNow
            };

            context.Messages.Add(message);
            await context.SaveChangesAsync();
            
            // --- TEMPS RÉEL ---
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
                
                // --- CORRECTION : On remplit bien ces champs pour le temps réel ---
                HasAttachment = dto.DocumentId.HasValue,
                AttachmentId = dto.DocumentId,
                AttachmentName = attachmentName 
                // -----------------------------------------------------------------
            };

            string groupName = dto.ColisId.HasValue ? dto.ColisId.ToString() : dto.VehiculeId.ToString();
            await _hubContext.Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);

            // --- NOTIFICATION ---
            // Si c'est une note interne, on ne notifie pas le client
            if (!dto.IsInternal)
            {
                Guid? clientId = null;
                string itemRef = "";

                // Trouver le client concerné
                if (dto.ColisId.HasValue)
                {
                    var colis = await context.Colis.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == dto.ColisId);
                    clientId = colis?.ClientId; // L'ID du Client (entité Client)
                    itemRef = colis?.NumeroReference ?? "Colis";
                }
                // (Ajouter logique véhicule ici de la même façon)

                // Si l'expéditeur est un admin, on notifie le client
                if (sender.Role != RoleUtilisateur.Client && clientId.HasValue)
                {
                    // Trouver le User lié au Client
                    var clientUser = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == clientId);
                    if (clientUser != null)
                    {
                        // On crée la notif pour l'utilisateur du client
                        // NOTE : J'utilise le service de notif standard, mais il faudra peut-être l'adapter pour cibler un User spécifique autre que le courant
                        // Pour l'instant, c'est conceptuel.
                    }
                }
            }

            return message;
        }

        public async Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid? colisId, Guid? vehiculeId, Guid currentUserId)
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

        public async Task MarkAsReadAsync(Guid? colisId, Guid? vehiculeId, Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Marquer comme lus les messages qui ne viennent PAS de moi
            var query = context.Messages.Where(m => m.ExpediteurId != userId && !m.IsRead);

            if (colisId.HasValue) query = query.Where(m => m.ColisId == colisId);
            else if (vehiculeId.HasValue) query = query.Where(m => m.VehiculeId == vehiculeId);

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
    }
}