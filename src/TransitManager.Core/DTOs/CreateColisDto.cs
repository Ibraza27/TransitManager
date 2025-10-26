using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class CreateColisDto
    {
        public Guid ClientId { get; set; }
        public string Designation { get; set; } = string.Empty;
        public string DestinationFinale { get; set; } = string.Empty;
        public List<string> Barcodes { get; set; } = new();
        
        public int NombrePieces { get; set; } = 1;
        public decimal Volume { get; set; }
        public decimal ValeurDeclaree { get; set; }
        public decimal PrixTotal { get; set; }
        public string? Destinataire { get; set; }
        public string? TelephoneDestinataire { get; set; }
        public bool LivraisonADomicile { get; set; }
        public string? AdresseLivraison { get; set; }
        public bool EstFragile { get; set; }
        public bool ManipulationSpeciale { get; set; }
        public string? InstructionsSpeciales { get; set; }
        public TypeColis Type { get; set; }
        public TypeEnvoi TypeEnvoi { get; set; }

        // --- AJOUTS ---
        public Guid? ConteneurId { get; set; }
    }
}