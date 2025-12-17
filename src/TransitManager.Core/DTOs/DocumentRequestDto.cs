using System;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class DocumentRequestDto
    {
        public Guid EntityId { get; set; }
        public TypeDocument Type { get; set; }
        public Guid ClientId { get; set; }
        public Guid? ColisId { get; set; }
        public Guid? VehiculeId { get; set; }
        public string? Commentaire { get; set; }
    }
}
