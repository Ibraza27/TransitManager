using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    public class Conteneur : BaseEntity
    {
        // --- Champs privés pour chaque propriété ---
        private string _numeroDossier = string.Empty;
        private string? _numeroPlomb;
        private string? _nomCompagnie;
        private string? _nomTransitaire;
        private string _destination = string.Empty;
        private string _paysDestination = string.Empty;
        private StatutConteneur _statut;
        private DateTime? _dateReception;
        private DateTime? _dateChargement;
        private DateTime? _dateDepart;
        private DateTime? _dateArriveeDestination;
        private DateTime? _dateDedouanement;
        private string? _commentaires;

        // --- Propriétés publiques utilisant SetProperty pour notifier les changements ---
        [Required]
        [StringLength(50)]
        public string NumeroDossier { get => _numeroDossier; set => SetProperty(ref _numeroDossier, value); }

        [StringLength(50)]
        public string? NumeroPlomb { get => _numeroPlomb; set => SetProperty(ref _numeroPlomb, value); }

        [StringLength(200)]
        public string? NomCompagnie { get => _nomCompagnie; set => SetProperty(ref _nomCompagnie, value); }

        [StringLength(200)]
        public string? NomTransitaire { get => _nomTransitaire; set => SetProperty(ref _nomTransitaire, value); }

        [Required]
        [StringLength(200)]
        public string Destination { get => _destination; set => SetProperty(ref _destination, value); }

        [Required]
        [StringLength(100)]
        public string PaysDestination { get => _paysDestination; set => SetProperty(ref _paysDestination, value); }
        
        public StatutConteneur Statut { get => _statut; set => SetProperty(ref _statut, value); }

        public DateTime? DateReception { get => _dateReception; set => SetProperty(ref _dateReception, value); }
        public DateTime? DateChargement { get => _dateChargement; set => SetProperty(ref _dateChargement, value); }
        public DateTime? DateDepart { get => _dateDepart; set => SetProperty(ref _dateDepart, value); }
        public DateTime? DateArriveeDestination { get => _dateArriveeDestination; set => SetProperty(ref _dateArriveeDestination, value); }
        public DateTime? DateDedouanement { get => _dateDedouanement; set => SetProperty(ref _dateDedouanement, value); }
		private DateTime? _dateCloture;
		public DateTime? DateCloture { get => _dateCloture; set => SetProperty(ref _dateCloture, value); }

        public string? Commentaires { get => _commentaires; set => SetProperty(ref _commentaires, value); }

        // Navigation properties
        public virtual ICollection<Colis> Colis { get; set; } = new List<Colis>();
        public virtual ICollection<Vehicule> Vehicules { get; set; } = new List<Vehicule>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();


        public Conteneur()
        {
            Statut = StatutConteneur.Reçu;
        }

        // --- Propriétés calculées (non mappées en base) ---
        public int NombreColis => Colis?.Count ?? 0;
        public int NombreVehicules => Vehicules?.Count ?? 0;
        
        public IEnumerable<Client> ClientsDistincts =>
            (Colis?.Select(c => c.Client) ?? Enumerable.Empty<Client?>())
            .Union(Vehicules?.Select(v => v.Client) ?? Enumerable.Empty<Client?>())
            .Where(c => c != null)
            .Select(c => c!) 
            .DistinctBy(c => c.Id);
    }
}