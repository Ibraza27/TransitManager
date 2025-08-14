// FONCTION COMPLÈTE MODIFIÉE (Colis.cs)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    public class Colis : BaseEntity
    {
        // --- Champs privés ---
        private string _numeroReference = string.Empty;
        private Guid _clientId;
        private Guid? _conteneurId;
        private DateTime _dateArrivee;
        private EtatColis _etat;
        private StatutColis _statut;
        private TypeColis _type;
        private int _nombrePieces = 1;
        private string _designation = string.Empty;
        private decimal _poids;
        private decimal _longueur;
        private decimal _largeur;
        private decimal _hauteur;
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
        private string _destinationFinale = string.Empty;
        private TypeEnvoi _typeEnvoi;
        private bool _livraisonADomicile;
        private decimal _prixTotal;
        private decimal _sommePayee; // CHAMP AJOUTÉ
        private decimal _restantAPayer; // CHAMP AJOUTÉ

        // --- Propriétés Publiques ---
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
        public string Designation { get => _designation; set => SetProperty(ref _designation, value); }

        public decimal Poids { get => _poids; set => SetProperty(ref _poids, value); }
        public decimal Longueur { get => _longueur; set => SetProperty(ref _longueur, value); }
        public decimal Largeur { get => _largeur; set => SetProperty(ref _largeur, value); }
        public decimal Hauteur { get => _hauteur; set => SetProperty(ref _hauteur, value); }
        public decimal ValeurDeclaree { get => _valeurDeclaree; set => SetProperty(ref _valeurDeclaree, value); }
        public bool EstFragile { get => _estFragile; set => SetProperty(ref _estFragile, value); }
        public bool ManipulationSpeciale { get => _manipulationSpeciale; set => SetProperty(ref _manipulationSpeciale, value); }
        
        [StringLength(1000)]
        public string? InstructionsSpeciales { get => _instructionsSpeciales; set => SetProperty(ref _instructionsSpeciales, value); }
        
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
        
        [Required]
        [StringLength(200)]
        public string DestinationFinale { get => _destinationFinale; set => SetProperty(ref _destinationFinale, value); }

        public TypeEnvoi TypeEnvoi { get => _typeEnvoi; set => SetProperty(ref _typeEnvoi, value); }
        public bool LivraisonADomicile { get => _livraisonADomicile; set => SetProperty(ref _livraisonADomicile, value); }
        public decimal PrixTotal { get => _prixTotal; set { if (SetProperty(ref _prixTotal, value)) OnPropertyChanged(nameof(RestantAPayer)); } }
        
        // PROPRIÉTÉ AJOUTÉE
        public decimal SommePayee { get => _sommePayee; set { if (SetProperty(ref _sommePayee, value)) OnPropertyChanged(nameof(RestantAPayer)); } }
        
        // PROPRIÉTÉ CALCULÉE AJOUTÉE (pas de `SetProperty` car elle dépend des autres)
        public decimal RestantAPayer => PrixTotal - SommePayee;

        public virtual Client? Client { get; set; }
        public virtual Conteneur? Conteneur { get; set; }

        public decimal Volume => (Longueur * Largeur * Hauteur) / 1000000m;
        public decimal PoidsVolumetrique => Volume * 167;
        public decimal PoidsFacturable => Math.Max(Poids, PoidsVolumetrique);

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