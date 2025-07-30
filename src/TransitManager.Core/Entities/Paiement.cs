using System;
using System.ComponentModel.DataAnnotations;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    /// <summary>
    /// Représente un paiement dans le système
    /// </summary>
    public class Paiement : BaseEntity
    {
        /// <summary>
        /// Numéro de reçu unique
        /// </summary>
        [Required]
        [StringLength(50)]
        public string NumeroRecu { get; set; } = string.Empty;

        /// <summary>
        /// ID du client
        /// </summary>
        public Guid ClientId { get; set; }

        /// <summary>
        /// ID du conteneur (si paiement lié à un conteneur spécifique)
        /// </summary>
        public Guid? ConteneurId { get; set; }

        /// <summary>
        /// ID de la facture associée
        /// </summary>
        public Guid? FactureId { get; set; }

        /// <summary>
        /// Date du paiement
        /// </summary>
        public DateTime DatePaiement { get; set; }

        /// <summary>
        /// Montant du paiement
        /// </summary>
        public decimal Montant { get; set; }

        /// <summary>
        /// Devise
        /// </summary>
        [Required]
        [StringLength(3)]
        public string Devise { get; set; } = "EUR";

        /// <summary>
        /// Taux de change appliqué
        /// </summary>
        public decimal TauxChange { get; set; } = 1;

        /// <summary>
        /// Montant en devise locale
        /// </summary>
        public decimal MontantLocal => Montant * TauxChange;

        /// <summary>
        /// Mode de paiement
        /// </summary>
        public TypePaiement ModePaiement { get; set; }

        /// <summary>
        /// Référence du paiement (numéro de chèque, virement, etc.)
        /// </summary>
        [StringLength(100)]
        public string? Reference { get; set; }

        /// <summary>
        /// Banque (pour chèques et virements)
        /// </summary>
        [StringLength(100)]
        public string? Banque { get; set; }

        /// <summary>
        /// Statut du paiement
        /// </summary>
        public StatutPaiement Statut { get; set; }

        /// <summary>
        /// Description/objet du paiement
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Notes/commentaires
        /// </summary>
        public string? Commentaires { get; set; }

        /// <summary>
        /// Chemin vers le reçu scanné
        /// </summary>
        [StringLength(500)]
        public string? RecuScanne { get; set; }

        /// <summary>
        /// Date d'échéance (pour les paiements différés)
        /// </summary>
        public DateTime? DateEcheance { get; set; }

        /// <summary>
        /// Indique si un rappel a été envoyé
        /// </summary>
        public bool RappelEnvoye { get; set; }

        /// <summary>
        /// Date du dernier rappel
        /// </summary>
        public DateTime? DateDernierRappel { get; set; }

        // Navigation properties
        /// <summary>
        /// Client associé
        /// </summary>
        public virtual Client? Client { get; set; }

        /// <summary>
        /// Conteneur associé
        /// </summary>
        public virtual Conteneur? Conteneur { get; set; }

        /// <summary>
        /// Constructeur
        /// </summary>
        public Paiement()
        {
            DatePaiement = DateTime.UtcNow;
            NumeroRecu = GenerateNumeroRecu();
            Statut = StatutPaiement.Valide;
        }

        /// <summary>
        /// Génère un numéro de reçu unique
        /// </summary>
        private static string GenerateNumeroRecu()
        {
            var year = DateTime.Now.ToString("yyyy");
            var month = DateTime.Now.ToString("MM");
            var day = DateTime.Now.ToString("dd");
            var random = new Random().Next(1000, 9999);
            return $"REC-{year}{month}{day}-{random}";
        }

        /// <summary>
        /// Indique si le paiement est en retard
        /// </summary>
        public bool EstEnRetard => 
            DateEcheance.HasValue && 
            DateEcheance.Value < DateTime.UtcNow && 
            Statut != StatutPaiement.Paye;
    }

}