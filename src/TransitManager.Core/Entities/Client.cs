using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TransitManager.Core.Entities
{
    /// <summary>
    /// Représente un client dans le système de gestion de transit
    /// </summary>
    public class Client : BaseEntity
    {
        /// <summary>
        /// Code client unique auto-généré
        /// </summary>
        [Required]
        [StringLength(20)]
        public string CodeClient { get; set; } = string.Empty;

        /// <summary>
        /// Nom du client
        /// </summary>
        [Required]
        [StringLength(100)]
		private string _nom = string.Empty;
		[Required]
		[StringLength(100)]
		public string Nom
		{
			get => _nom;
			set => SetProperty(ref _nom, value);
		}

        /// <summary>
        /// Prénom du client
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Prenom { get; set; } = string.Empty;

        /// <summary>
        /// Téléphone principal
        /// </summary>
        [Required]
        [StringLength(20)]
        public string TelephonePrincipal { get; set; } = string.Empty;

        /// <summary>
        /// Téléphone secondaire
        /// </summary>
        [StringLength(20)]
        public string? TelephoneSecondaire { get; set; }

        /// <summary>
        /// Adresse email
        /// </summary>
        [EmailAddress]
        [StringLength(150)]
        public string? Email { get; set; }

        /// <summary>
        /// Adresse principale
        /// </summary>
        [StringLength(500)]
        public string? AdressePrincipale { get; set; }

        /// <summary>
        /// Adresse de livraison (si différente)
        /// </summary>
        [StringLength(500)]
        public string? AdresseLivraison { get; set; }

        /// <summary>
        /// Ville
        /// </summary>
        [StringLength(100)]
        public string? Ville { get; set; }

        /// <summary>
        /// Code postal
        /// </summary>
        [StringLength(20)]
        public string? CodePostal { get; set; }

        /// <summary>
        /// Pays
        /// </summary>
        [StringLength(100)]
        public string? Pays { get; set; }

        /// <summary>
        /// Date d'inscription
        /// </summary>
        public DateTime DateInscription { get; set; }

        /// <summary>
        /// Notes et commentaires spéciaux
        /// </summary>
        public string? Commentaires { get; set; }

        /// <summary>
        /// Photo ou scan de la pièce d'identité (chemin du fichier)
        /// </summary>
        [StringLength(500)]
        public string? PieceIdentite { get; set; }

        /// <summary>
        /// Type de pièce d'identité
        /// </summary>
        [StringLength(50)]
        public string? TypePieceIdentite { get; set; }

        /// <summary>
        /// Numéro de la pièce d'identité
        /// </summary>
        [StringLength(100)]
        public string? NumeroPieceIdentite { get; set; }

        /// <summary>
        /// Indique si le client est un client fidèle
        /// </summary>
        public bool EstClientFidele { get; set; }

        /// <summary>
        /// Pourcentage de remise accordée
        /// </summary>
        public decimal PourcentageRemise { get; set; }

        /// <summary>
        /// Balance totale du client (montant dû)
        /// </summary>
        public decimal BalanceTotal { get; set; }

        /// <summary>
        /// Nombre total d'envois
        /// </summary>
        public int NombreTotalEnvois { get; set; }

        /// <summary>
        /// Volume total expédié (en m³)
        /// </summary>
        public decimal VolumeTotalExpedié { get; set; }

        // Navigation properties
        /// <summary>
        /// Liste des colis du client
        /// </summary>
        public virtual ICollection<Colis> Colis { get; set; } = new List<Colis>();

        /// <summary>
        /// Liste des paiements du client
        /// </summary>
        public virtual ICollection<Paiement> Paiements { get; set; } = new List<Paiement>();

        /// <summary>
        /// Constructeur
        /// </summary>
        public Client()
        {
            DateInscription = DateTime.UtcNow;
            CodeClient = GenerateCodeClient();
        }

        /// <summary>
        /// Génère un code client unique
        /// </summary>
        private static string GenerateCodeClient()
        {
            // Format: CLI-YYYYMMDD-XXXX (où XXXX est un nombre aléatoire)
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"CLI-{date}-{random}";
        }

        /// <summary>
        /// Nom complet du client
        /// </summary>
        public string NomComplet => $"{Nom} {Prenom}";

        /// <summary>
        /// Adresse complète formatée
        /// </summary>
        public string AdresseComplete => 
            $"{AdressePrincipale}, {CodePostal} {Ville}, {Pays}";
    }
}