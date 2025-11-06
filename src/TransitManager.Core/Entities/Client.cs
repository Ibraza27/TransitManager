using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TransitManager.Core.Entities
{
    public class Client : BaseEntity
    {
        // --- Champs privés pour chaque propriété ---
        private string _codeClient = string.Empty;
        private string _nom = string.Empty;
        private string _prenom = string.Empty;
        private string _telephonePrincipal = string.Empty;
        private string? _telephoneSecondaire;
        private string? _email;
        private string? _adressePrincipale;
        private string? _adresseLivraison;
        private string? _ville;
        private string? _codePostal;
        private string? _pays;
        private string? _commentaires;
        private string? _pieceIdentite;
        private string? _typePieceIdentite;
        private string? _numeroPieceIdentite;
        private bool _estClientFidele;
        private decimal _pourcentageRemise;
        private decimal _impayes;
		private int _nombreConteneursUniques;
        private int _nombreTotalEnvois;
		public int NombreTotalEnvois { get => _nombreTotalEnvois; set => SetProperty(ref _nombreTotalEnvois, value); }
        private decimal _volumeTotalExpedie;

        // --- Propriétés publiques utilisant SetProperty ---
        [Required]
        [StringLength(20)]
        public string CodeClient { get => _codeClient; set => SetProperty(ref _codeClient, value); }
        
        [Required]
        [StringLength(100)]
        public string Nom { get => _nom; set => SetProperty(ref _nom, value); }

        [Required]
        [StringLength(100)]
        public string Prenom { get => _prenom; set => SetProperty(ref _prenom, value); }
        
        [Required]
        [StringLength(20)]
        public string TelephonePrincipal { get => _telephonePrincipal; set => SetProperty(ref _telephonePrincipal, value); }

        [StringLength(20)]
        public string? TelephoneSecondaire { get => _telephoneSecondaire; set => SetProperty(ref _telephoneSecondaire, value); }
        
        [EmailAddress]
        [StringLength(150)]
        public string? Email { get => _email; set => SetProperty(ref _email, value); }
        
        [StringLength(500)]
        public string? AdressePrincipale { get => _adressePrincipale; set => SetProperty(ref _adressePrincipale, value); }
        
        [StringLength(500)]
        public string? AdresseLivraison { get => _adresseLivraison; set => SetProperty(ref _adresseLivraison, value); }
        
        [StringLength(100)]
        public string? Ville { get => _ville; set => SetProperty(ref _ville, value); }
        
        [StringLength(20)]
        public string? CodePostal { get => _codePostal; set => SetProperty(ref _codePostal, value); }
        
        [StringLength(100)]
        public string? Pays { get => _pays; set => SetProperty(ref _pays, value); }
        
        public DateTime DateInscription { get; set; }
        
        public string? Commentaires { get => _commentaires; set => SetProperty(ref _commentaires, value); }
        
        [StringLength(500)]
        public string? PieceIdentite { get => _pieceIdentite; set => SetProperty(ref _pieceIdentite, value); }
        
        [StringLength(50)]
        public string? TypePieceIdentite { get => _typePieceIdentite; set => SetProperty(ref _typePieceIdentite, value); }

        [StringLength(100)]
        public string? NumeroPieceIdentite { get => _numeroPieceIdentite; set => SetProperty(ref _numeroPieceIdentite, value); }
        
        public bool EstClientFidele { get => _estClientFidele; set => SetProperty(ref _estClientFidele, value); }
        
        public decimal PourcentageRemise { get => _pourcentageRemise; set => SetProperty(ref _pourcentageRemise, value); }
        
        public decimal Impayes { get => _impayes; set => SetProperty(ref _impayes, value); }
        
        public int NombreConteneursUniques { get => _nombreConteneursUniques; set => SetProperty(ref _nombreConteneursUniques, value); }
        
        public decimal VolumeTotalExpedié { get => _volumeTotalExpedie; set => SetProperty(ref _volumeTotalExpedie, value); }
	

        // --- DÉBUT DE LA MODIFICATION ---
        // --- Propriétés de navigation ---
        public virtual ICollection<Colis> Colis { get; set; } = new List<Colis>();
		public virtual ICollection<Vehicule> Vehicules { get; set; } = new List<Vehicule>();
        public virtual ICollection<Paiement> Paiements { get; set; } = new List<Paiement>();
        // --- FIN DE LA MODIFICATION ---
        
        // --- Constructeur et Méthodes ---
        public Client()
        {
            DateInscription = DateTime.UtcNow;
            CodeClient = GenerateCodeClient();
        }

        private static string GenerateCodeClient()
        {
            var date = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"CLI-{date}-{random}";
        }
        
        public string NomComplet => $"{Nom} {Prenom}";
        public string AdresseComplete => $"{AdressePrincipale}, {CodePostal} {Ville}, {Pays}";
    }
}