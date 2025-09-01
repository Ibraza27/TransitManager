using System;
using System.ComponentModel.DataAnnotations;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    public class Paiement : BaseEntity
    {
        // --- CHAMPS PRIVÉS ---
        private string _numeroRecu = string.Empty;
        private Guid _clientId;
        private Guid? _colisId; // Ajouté pour la nouvelle fonctionnalité
		private Guid? _vehiculeId;
        private Guid? _conteneurId;
        private Guid? _factureId;
        private DateTime _datePaiement;
        private decimal _montant;
        private string _devise = "EUR";
        private decimal _tauxChange = 1;
        private TypePaiement _modePaiement;
        private string? _reference;
        private string? _banque;
        private StatutPaiement _statut;
        private string? _description;
        private string? _commentaires;
        private string? _recuScanne;
        private DateTime? _dateEcheance;
        private bool _rappelEnvoye;
        private DateTime? _dateDernierRappel;

        // --- PROPRIÉTÉS PUBLIQUES ---
        [Required]
        [StringLength(50)]
        public string NumeroRecu { get => _numeroRecu; set => SetProperty(ref _numeroRecu, value); }

        public Guid ClientId { get => _clientId; set => SetProperty(ref _clientId, value); }
        public Guid? ColisId { get => _colisId; set => SetProperty(ref _colisId, value); } // Ajouté
		//public Guid? VehiculeId { get; private set; }
		public Guid? VehiculeId { get => _vehiculeId; set => SetProperty(ref _vehiculeId, value); }
		public Guid? ConteneurId { get => _conteneurId; set => SetProperty(ref _conteneurId, value); }
        public Guid? FactureId { get => _factureId; set => SetProperty(ref _factureId, value); }
        public DateTime DatePaiement { get => _datePaiement; set => SetProperty(ref _datePaiement, value); }
        public decimal Montant { get => _montant; set => SetProperty(ref _montant, value); }

        [Required]
        [StringLength(3)]
        public string Devise { get => _devise; set => SetProperty(ref _devise, value); }
        public decimal TauxChange { get => _tauxChange; set => SetProperty(ref _tauxChange, value); }
        public TypePaiement ModePaiement { get => _modePaiement; set => SetProperty(ref _modePaiement, value); }

        [StringLength(100)]
        public string? Reference { get => _reference; set => SetProperty(ref _reference, value); }

        [StringLength(100)]
        public string? Banque { get => _banque; set => SetProperty(ref _banque, value); }

        public StatutPaiement Statut { get => _statut; set => SetProperty(ref _statut, value); }

        [StringLength(500)]
        public string? Description { get => _description; set => SetProperty(ref _description, value); }
        public string? Commentaires { get => _commentaires; set => SetProperty(ref _commentaires, value); }

        [StringLength(500)]
        public string? RecuScanne { get => _recuScanne; set => SetProperty(ref _recuScanne, value); }

        public DateTime? DateEcheance { get => _dateEcheance; set => SetProperty(ref _dateEcheance, value); }
        public bool RappelEnvoye { get => _rappelEnvoye; set => SetProperty(ref _rappelEnvoye, value); }
        public DateTime? DateDernierRappel { get => _dateDernierRappel; set => SetProperty(ref _dateDernierRappel, value); }

        // Propriétés calculées
        public decimal MontantLocal => Montant * TauxChange;
        public bool EstEnRetard => DateEcheance.HasValue && DateEcheance.Value < DateTime.UtcNow && Statut != StatutPaiement.Paye;

        // Navigation properties
        public virtual Client? Client { get; set; }
        public virtual Colis? Colis { get; set; }
		public virtual Vehicule? Vehicule { get; set; }
		public virtual Conteneur? Conteneur { get; set; }

        public Paiement()
        {
            DatePaiement = DateTime.Now; // Modifié pour la date du jour locale
            NumeroRecu = GenerateNumeroRecu();
            Statut = StatutPaiement.Valide;
        }

        private static string GenerateNumeroRecu()
        {
            var year = DateTime.Now.ToString("yyyy");
            var month = DateTime.Now.ToString("MM");
            var day = DateTime.Now.ToString("dd");
            var random = new Random().Next(1000, 9999);
            return $"REC-{year}{month}{day}-{random}";
        }
    }
}