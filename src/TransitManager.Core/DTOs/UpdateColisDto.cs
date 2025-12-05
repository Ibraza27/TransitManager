using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class UpdateColisDto : CreateColisDto
    {
        public Guid Id { get; set; }

        // --- AJOUTS ---
        public StatutColis Statut { get; set; }
        public decimal SommePayee { get; set; }
		public string? LieuSignatureInventaire { get; set; }
        public DateTime? DateSignatureInventaire { get; set; }
        public string? SignatureClientInventaire { get; set; }
    }
}