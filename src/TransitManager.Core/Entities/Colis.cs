using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    public class Colis : BaseEntity
    {
        private string _numeroReference = string.Empty;
        private Guid _clientId;
        private Guid? _conteneurId;
        private DateTime _dateArrivee;
        private EtatColis _etat;
        private StatutColis _statut;
        private TypeColis _type;
        private int _nombrePieces = 1;
        private string _designation = string.Empty;
        private decimal _volume; // AJOUTÉ
        private decimal _valeurDeclaree;
        private bool _estFragile;
        private bool _manipulationSpeciale;
        private string? _instructionsSpeciales;
        private string? _photos;
        private DateTime? _dateDernierScan;
        private string? _localisationActuelle;
        private string? _historiqueScan;
        private DateTime? _dateLivraison;
        private string? _destinataire;
        private string? _signatureReception;
        private string? _commentaires;
        private string? _telephoneDestinataire;
		private string? _adresseLivraison;
        private string _destinationFinale = string.Empty;
        private TypeEnvoi _typeEnvoi;
        private bool _livraisonADomicile;
        private decimal _prixTotal;
        private decimal _sommePayee;
        
        // --- NOUVEAU CHAMP ---
        private string? _numeroPlomb;
		private string? _inventaireJson;

        public virtual ICollection<Barcode> Barcodes { get; set; } = new List<Barcode>();

        [Required]
        [StringLength(50)]
        public string NumeroReference { get => _numeroReference; set => SetProperty(ref _numeroReference, value); }
        public Guid ClientId { get => _clientId; set => SetProperty(ref _clientId, value); }
        public Guid? ConteneurId { get => _conteneurId; set => SetProperty(ref _conteneurId, value); }
        public DateTime DateArrivee { get => _dateArrivee; set => SetProperty(ref _dateArrivee, value); }
        public EtatColis Etat { get => _etat; set => SetProperty(ref _etat, value); }
        public StatutColis Statut { get => _statut; set => SetProperty(ref _statut, value); }
        public TypeColis Type { get => _type; set => SetProperty(ref _type, value); }
        public int NombrePieces { get => _nombrePieces; set => SetProperty(ref _nombrePieces, value); }
        
		[Required]
		[StringLength(500)]
		public string Designation 
		{ 
			get => _designation; 
			// MODIFIER LE SETTER
			set => SetProperty(ref _designation, value?.ToUpper()); 
		}

        public decimal Volume { get => _volume; set => SetProperty(ref _volume, value); } // AJOUTÉ

        public decimal ValeurDeclaree { get => _valeurDeclaree; set => SetProperty(ref _valeurDeclaree, value); }
        public bool EstFragile { get => _estFragile; set => SetProperty(ref _estFragile, value); }
        public bool ManipulationSpeciale { get => _manipulationSpeciale; set => SetProperty(ref _manipulationSpeciale, value); }
        
		public string? InstructionsSpeciales 
		{ 
			get => _instructionsSpeciales; 
			// MODIFIER LE SETTER
			set => SetProperty(ref _instructionsSpeciales, value?.ToUpper()); 
		}
        
        public string? Photos { get => _photos; set => SetProperty(ref _photos, value); }
        public DateTime? DateDernierScan { get => _dateDernierScan; set => SetProperty(ref _dateDernierScan, value); }
        [StringLength(200)]
        public string? LocalisationActuelle { get => _localisationActuelle; set => SetProperty(ref _localisationActuelle, value); }
        public string? HistoriqueScan { get => _historiqueScan; set => SetProperty(ref _historiqueScan, value); }
        public DateTime? DateLivraison { get => _dateLivraison; set => SetProperty(ref _dateLivraison, value); }
        public string? SignatureReception { get => _signatureReception; set => SetProperty(ref _signatureReception, value); }
        public string? Commentaires { get => _commentaires; set => SetProperty(ref _commentaires, value); }

        [StringLength(200)]
        public string? Destinataire { get => _destinataire; set => SetProperty(ref _destinataire, value); }
        
        [StringLength(20)]
        public string? TelephoneDestinataire { get => _telephoneDestinataire; set => SetProperty(ref _telephoneDestinataire, value); }
		
		[StringLength(500)]
		public string? AdresseLivraison 
		{ 
			get => _adresseLivraison; 
			set => SetProperty(ref _adresseLivraison, value?.ToUpper()); 
		}		
        
		[Required]
		[StringLength(200)]
		public string DestinationFinale 
		{ 
			get => _destinationFinale; 
			// MODIFIER LE SETTER
			set => SetProperty(ref _destinationFinale, value?.ToUpper()); 
		}

        public TypeEnvoi TypeEnvoi { get => _typeEnvoi; set => SetProperty(ref _typeEnvoi, value); }
        public bool LivraisonADomicile { get => _livraisonADomicile; set => SetProperty(ref _livraisonADomicile, value); }
        public decimal PrixTotal { get => _prixTotal; set { if (SetProperty(ref _prixTotal, value)) OnPropertyChanged(nameof(RestantAPayer)); } }
        
        public decimal SommePayee { get => _sommePayee; set { if (SetProperty(ref _sommePayee, value)) OnPropertyChanged(nameof(RestantAPayer)); } }
        
        public decimal RestantAPayer => PrixTotal - SommePayee;

        // --- NOUVELLE PROPRIÉTÉ ---
        [StringLength(50)]
        public string? NumeroPlomb { get => _numeroPlomb; set => SetProperty(ref _numeroPlomb, value); }
		
		public string? InventaireJson { get => _inventaireJson; set => SetProperty(ref _inventaireJson, value); }
        
        public virtual Client? Client { get; set; }
        public virtual Conteneur? Conteneur { get; set; }
		
		public virtual ICollection<Paiement> Paiements { get; set; } = new List<Paiement>();

        // public decimal Volume => (Longueur * Largeur * Hauteur) / 1000000m; // MODIFIÉ (devient une propriété normale)
        // public decimal PoidsVolumetrique => Volume * 167; // SUPPRIMÉ ou commenté
        // public decimal PoidsFacturable => Math.Max(Poids, PoidsVolumetrique); // SUPPRIMÉ ou commenté
		
        /// <summary>
        /// Propriété calculée qui indique si le colis est en attente depuis plus de 5 jours.
        /// Non mappée en base de données.
        /// </summary>
        public bool EstEnRetard => 
            Statut == StatutColis.EnAttente && (DateTime.UtcNow - DateArrivee).TotalDays > 5;


        public Colis()
        {
            DateArrivee = DateTime.UtcNow;
            NumeroReference = GenerateReference();
            Statut = StatutColis.EnAttente;
            Etat = EtatColis.BonEtat;
        }

        private static string GenerateReference()
        {
            return $"REF-{DateTime.Now:yyyyMMdd}-{new Random().Next(10000, 99999)}";
        }
		
		public string FirstBarcode => Barcodes?.FirstOrDefault()?.Value ?? "N/A";
        public string AllBarcodes => Barcodes != null && Barcodes.Any() ? string.Join(", ", Barcodes.Select(b => b.Value)) : "N/A";
    }
}