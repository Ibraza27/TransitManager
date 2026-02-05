using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class ColisListItemDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; } // AJOUT
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
        public decimal FraisDouane { get; set; } // AJOUT
        public TypeEnvoi TypeEnvoi { get; set; } // AJOUT
        public decimal SommePayee { get; set; }
        public bool IsExcludedFromExport { get; set; }
        
        // Propriété calculée simple (le calcul réel se fera lors du mapping)
        public decimal TotalFinal => TypeEnvoi == TypeEnvoi.AvecDedouanement ? PrixTotal + FraisDouane : PrixTotal;
        public decimal RestantAPayer => TotalFinal - SommePayee;
    }
}