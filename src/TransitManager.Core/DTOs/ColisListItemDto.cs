using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class ColisListItemDto
    {
        public Guid Id { get; set; }
        public string NumeroReference { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public StatutColis Statut { get; set; }
        public bool HasMissingDocuments { get; set; }
        public string ClientNomComplet { get; set; } = string.Empty;
        public string? ClientTelephonePrincipal { get; set; }
        public string? ConteneurNumeroDossier { get; set; }
        public string AllBarcodes { get; set; } = string.Empty;
        public string DestinationFinale { get; set; } = string.Empty;
        public DateTime DateArrivee { get; set; }
        
        public Guid? ConteneurId { get; set; }
        public string? ConteneurNumero { get; set; }
        public decimal Volume { get; set; }

        // --- AJOUTS POUR LA VUE WEB ---
        public int NombrePieces { get; set; }
        public decimal PrixTotal { get; set; }
        public decimal SommePayee { get; set; }
        
        // Propriété calculée simple (le calcul réel se fera lors du mapping)
        public decimal RestantAPayer => PrixTotal - SommePayee;
    }
}