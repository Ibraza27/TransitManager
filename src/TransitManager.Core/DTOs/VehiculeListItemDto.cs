using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class VehiculeListItemDto
    {
        public Guid Id { get; set; }
        public string Immatriculation { get; set; } = string.Empty;
        public string Marque { get; set; } = string.Empty;
        public string Modele { get; set; } = string.Empty;
        public StatutVehicule Statut { get; set; }
        public string ClientNomComplet { get; set; } = string.Empty;
        public string? ConteneurNumeroDossier { get; set; }
		public DateTime DateCreation { get; set; }
    }
}