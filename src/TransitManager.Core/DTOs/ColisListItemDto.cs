using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class ColisListItemDto
    {
        public Guid Id { get; set; }
        public string NumeroReference { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public StatutColis Statut { get; set; }
        public string ClientNomComplet { get; set; } = string.Empty;
        public string? ConteneurNumeroDossier { get; set; }
		public string AllBarcodes { get; set; } = string.Empty;
        
        // --- DÉBUT DE L'AJOUT ---
        public string DestinationFinale { get; set; } = string.Empty;
        public DateTime DateArrivee { get; set; }
        // --- FIN DE L'AJOUT ---
    }
}