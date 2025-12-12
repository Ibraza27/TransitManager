using System;

namespace TransitManager.Core.DTOs
{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public string Contenu { get; set; } = string.Empty;
        public DateTime DateEnvoi { get; set; }
        
        // Infos Expéditeur
        public string NomExpediteur { get; set; } = string.Empty;
        public bool EstMoi { get; set; } // Pour l'affichage (bulles droite/gauche)
        public bool EstAdmin { get; set; } // Pour savoir si c'est un message officiel

        // Options
        public bool IsInternal { get; set; }
        public bool IsRead { get; set; }

        // Pièce jointe
        public bool HasAttachment { get; set; }
        public string? AttachmentName { get; set; }
        public Guid? AttachmentId { get; set; }
    }

    public class CreateMessageDto
    {
        public string Contenu { get; set; } = string.Empty;
        public Guid? ColisId { get; set; }
        public Guid? VehiculeId { get; set; }
		public Guid? ConteneurId { get; set; }
        public bool IsInternal { get; set; }
        
        // ID d'un document déjà uploadé à lier
        public Guid? DocumentId { get; set; } 
    }
}