using System;
using System.ComponentModel.DataAnnotations;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    /// <summary>
    /// Représente un colis/marchandise dans le système
    /// </summary>
    public class Colis : BaseEntity
    {
        /// <summary>
        /// Code-barres unique du colis
        /// </summary>
        [Required]
        [StringLength(50)]
        public string CodeBarre { get; set; } = string.Empty;

        /// <summary>
        /// Numéro de référence interne
        /// </summary>
        [Required]
        [StringLength(50)]
        public string NumeroReference { get; set; } = string.Empty;

        /// <summary>
        /// ID du client propriétaire
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        /// ID du conteneur (si affecté)
        /// </summary>
        public Guid? ConteneurId { get; set; }

        /// <summary>
        /// Date d'arrivée/réception
        /// </summary>
        public DateTime DateArrivee { get; set; }

        /// <summary>
        /// État du colis
        /// </summary>
        public EtatColis Etat { get; set; }

        /// <summary>
        /// Statut actuel du colis
        /// </summary>
        public StatutColis Statut { get; set; }

        /// <summary>
        /// Type de colis
        /// </summary>
        public TypeColis Type { get; set; }

        /// <summary>
        /// Nombre de pièces dans le lot
        /// </summary>
        public int NombrePieces { get; set; } = 1;

        /// <summary>
        /// Désignation/description détaillée
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Designation { get; set; } = string.Empty;

        /// <summary>
        /// Poids en kilogrammes
        /// </summary>
        public decimal Poids { get; set; }

        /// <summary>
        /// Longueur en centimètres
        /// </summary>
        public decimal Longueur { get; set; }

        /// <summary>
        /// Largeur en centimètres
        /// </summary>
        public decimal Largeur { get; set; }

        /// <summary>
        /// Hauteur en centimètres
        /// </summary>
        public decimal Hauteur { get; set; }

        /// <summary>
        /// Volume calculé en m³
        /// </summary>
        public decimal Volume => (Longueur * Largeur * Hauteur) / 1000000m;

        /// <summary>
        /// Valeur déclarée pour l'assurance
        /// </summary>
        public decimal ValeurDeclaree { get; set; }

        /// <summary>
        /// Indique si le colis est fragile
        /// </summary>
        public bool EstFragile { get; set; }

        /// <summary>
        /// Indique si le colis nécessite une manipulation spéciale
        /// </summary>
        public bool ManipulationSpeciale { get; set; }

        /// <summary>
        /// Instructions spéciales de manipulation
        /// </summary>
        [StringLength(1000)]
        public string? InstructionsSpeciales { get; set; }

        /// <summary>
        /// Photos du colis (chemins séparés par des virgules)
        /// </summary>
        public string? Photos { get; set; }

        /// <summary>
        /// Date de dernière scan
        /// </summary>
        public DateTime? DateDernierScan { get; set; }

        /// <summary>
        /// Localisation actuelle
        /// </summary>
        [StringLength(200)]
        public string? LocalisationActuelle { get; set; }

        /// <summary>
        /// Historique des scans (JSON)
        /// </summary>
        public string? HistoriqueScan { get; set; }

        /// <summary>
        /// Date de livraison
        /// </summary>
        public DateTime? DateLivraison { get; set; }

        /// <summary>
        /// Nom du destinataire final
        /// </summary>
        [StringLength(200)]
        public string? Destinataire { get; set; }

        /// <summary>
        /// Signature de réception (base64)
        /// </summary>
        public string? SignatureReception { get; set; }

        /// <summary>
        /// Commentaires/notes
        /// </summary>
        public string? Commentaires { get; set; }

        // Navigation properties
        /// <summary>
        /// Client propriétaire
        /// </summary>
        public virtual Client? Client { get; set; }

        /// <summary>
        /// Conteneur associé
        /// </summary>
        public virtual Conteneur? Conteneur { get; set; }

        /// <summary>
        /// Constructeur
        /// </summary>
        public Colis()
        {
            DateArrivee = DateTime.UtcNow;
            NumeroReference = GenerateReference();
            CodeBarre = GenerateBarcode();
            Statut = StatutColis.EnAttente;
            Etat = EtatColis.BonEtat;
        }

        /// <summary>
        /// Génère une référence unique
        /// </summary>
        private static string GenerateReference()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(10000, 99999);
            return $"REF-{date}-{random}";
        }

        /// <summary>
        /// Génère un code-barres unique
        /// </summary>
        private static string GenerateBarcode()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var random = new Random().Next(1000, 9999);
            return $"{timestamp}{random}";
        }

        /// <summary>
        /// Calcule le poids volumétrique
        /// </summary>
        public decimal PoidsVolumetrique => Volume * 167; // Standard industrie : 1m³ = 167kg

        /// <summary>
        /// Poids facturable (le plus élevé entre poids réel et volumétrique)
        /// </summary>
        public decimal PoidsFacturable => Math.Max(Poids, PoidsVolumetrique);
    }


}