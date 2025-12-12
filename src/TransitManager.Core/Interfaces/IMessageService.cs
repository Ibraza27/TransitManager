using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;

namespace TransitManager.Core.Interfaces
{
    public interface IMessageService
    {
        // Récupérer la discussion
        Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid? colisId, Guid? vehiculeId, Guid? conteneurId, Guid currentUserId);
        
        // Envoyer un message
        Task<Message> SendMessageAsync(CreateMessageDto dto, Guid senderId);
        
        // Marquer comme lu
        Task MarkAsReadAsync(Guid? colisId, Guid? vehiculeId, Guid? conteneurId, Guid userId);
    }
}