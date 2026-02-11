using System;
using System.Collections.Generic; // Ajouté pour ICollection
using System.ComponentModel.DataAnnotations;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    public class Vehicule : BaseEntity
    {
        private Guid _clientId;
        private string _immatriculation = string.Empty;
        private string _marque = string.Empty;
        private string _modele = string.Empty;
        private int _annee;
        private int _kilometrage;
        private string _destinationFinale = string.Empty;
        private decimal _valeurDeclaree;
        private string? _destinataire;
        private string? _telephoneDestinataire;
        private TypeVehicule _type;
        private string? _commentaires;
        private decimal _prixTotal;
        private decimal _sommePayee;
        
        private StatutVehicule _statut;
        private Guid? _conteneurId;
        private string? _numeroPlomb;
        
        // --- NOUVEAUX CHAMPS ÉTAT DES LIEUX ---
        private string? _lieuEtatDesLieux;
        private DateTime? _dateEtatDesLieux;
        private string? _signatureAgent; // Base64
        private string? _signatureClient; // Base64
        private string? _accessoiresJson; // JSON des équipements

        public Guid ClientId { get => _clientId; set => SetProperty(ref _clientId, value); }

        [Required]
        [StringLength(50)]
        public string Immatriculation { get => _immatriculation; set => SetProperty(ref _immatriculation, value); }

        [Required]
        [StringLength(100)]
        public string Marque { get => _marque; set => SetProperty(ref _marque, value); }

        [Required]
        [StringLength(100)]
        public string Modele { get => _modele; set => SetProperty(ref _modele, value); }

        public int Annee { get => _annee; set => SetProperty(ref _annee, value); }
        public int Kilometrage { get => _kilometrage; set => SetProperty(ref _kilometrage, value); }

        [Required]
        [StringLength(200)]
        public string DestinationFinale { get => _destinationFinale; set => SetProperty(ref _destinationFinale, value); }
        
        private string? _adresseFrance;
        private string? _adresseDestination;
        
        [StringLength(500)]
        public string? AdresseFrance { get => _adresseFrance; set => SetProperty(ref _adresseFrance, value); }
        
        [StringLength(500)]
        public string? AdresseDestination { get => _adresseDestination; set => SetProperty(ref _adresseDestination, value); }

        public decimal ValeurDeclaree { get => _valeurDeclaree; set => SetProperty(ref _valeurDeclaree, value); }

        [StringLength(200)]
        public string? Destinataire { get => _destinataire; set => SetProperty(ref _destinataire, value); }
        
        [StringLength(20)]
        public string? TelephoneDestinataire { get => _telephoneDestinataire; set => SetProperty(ref _telephoneDestinataire, value); }

        public TypeVehicule Type { get => _type; set => SetProperty(ref _type, value); }
        
        private Motorisation _motorisation;
        public Motorisation Motorisation { get => _motorisation; set => SetProperty(ref _motorisation, value); }

        public string? Commentaires { get => _commentaires; set => SetProperty(ref _commentaires, value); }
        
        public bool HasAssurance { get; set; } // AJOUT: Assurance optionnelle

        public decimal PrixTotal
        {
            get => _prixTotal;
            set { if (SetProperty(ref _prixTotal, value)) { OnPropertyChanged(nameof(RestantAPayer)); } }
        }

        public decimal SommePayee
        {
            get => _sommePayee;
            set { if (SetProperty(ref _sommePayee, value)) { OnPropertyChanged(nameof(RestantAPayer)); } }
        }

        public decimal MontantAssurance
        {
            get
            {
                if (!HasAssurance) return 0;
                var baseAmount = (ValeurDeclaree + PrixTotal) * 1.2m; // +20%
                var assurance = (baseAmount * 0.007m) + 50m; // 0.7% + 50€
                return assurance < 250m ? 250m : assurance;
            }
        }

        public decimal TotalFinal => PrixTotal + MontantAssurance;

        public decimal RestantAPayer => Math.Max(0, TotalFinal - SommePayee);
        
        // Dimensions pour calcul de prix
        public int? DimensionsLongueurCm { get; set; }
        public int? DimensionsLargeurCm { get; set; }
        public int? DimensionsHauteurCm { get; set; }
        public bool IsPriceCalculated { get; set; }
        
        public string? EtatDesLieux { get; set; }
        public string? EtatDesLieuxRayures { get; set; }

        public StatutVehicule Statut { get => _statut; set => SetProperty(ref _statut, value); }
        public Guid? ConteneurId { get => _conteneurId; set => SetProperty(ref _conteneurId, value); }
        
        [StringLength(50)]
        public string? NumeroPlomb { get => _numeroPlomb; set => SetProperty(ref _numeroPlomb, value); }
        
        // --- PROPRIÉTÉS DE SIGNATURE ET ÉQUIPEMENTS ---
        [StringLength(100)]
        public string? LieuEtatDesLieux { get => _lieuEtatDesLieux; set => SetProperty(ref _lieuEtatDesLieux, value); }
        
        public DateTime? DateEtatDesLieux { get => _dateEtatDesLieux; set => SetProperty(ref _dateEtatDesLieux, value); }
        
        // Stockage Base64 des signatures (peut être lourd, TEXT en BDD)
        public string? SignatureAgent { get => _signatureAgent; set => SetProperty(ref _signatureAgent, value); }
        public string? SignatureClient { get => _signatureClient; set => SetProperty(ref _signatureClient, value); }
        
        public string? AccessoiresJson { get => _accessoiresJson; set => SetProperty(ref _accessoiresJson, value); }

        // --- RELATIONS DE NAVIGATION ---
        public virtual Client? Client { get; set; }
        public virtual Conteneur? Conteneur { get; set; }
        public virtual ICollection<Paiement> Paiements { get; set; } = new List<Paiement>();
        
        // Nouvelle relation pour les documents
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        
        public Vehicule()
        {
            Statut = StatutVehicule.EnAttente;
        }
    }
}