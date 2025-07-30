using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    /// <summary>
    /// Représente un conteneur/dossier d'expédition
    /// </summary>
    public class Conteneur : BaseEntity
    {
        /// <summary>
        /// Numéro unique du conteneur/dossier
        /// </summary>
        [Required]
        [StringLength(50)]
        public string NumeroDossier { get; set; } = string.Empty;

        /// <summary>
        /// Destination (port/ville)
        /// </summary>
        [Required]
        [StringLength(200)]
        public string Destination { get; set; } = string.Empty;

        /// <summary>
        /// Pays de destination
        /// </summary>
        [Required]
        [StringLength(100)]
        public string PaysDestination { get; set; } = string.Empty;

        /// <summary>
        /// Type d'envoi
        /// </summary>
        public TypeEnvoi TypeEnvoi { get; set; }

        /// <summary>
        /// Statut du conteneur
        /// </summary>
        public StatutConteneur Statut { get; set; }

        /// <summary>
        /// Date d'ouverture du dossier
        /// </summary>
        public DateTime DateOuverture { get; set; }

        /// <summary>
        /// Date de départ prévue
        /// </summary>
        public DateTime? DateDepartPrevue { get; set; }

        /// <summary>
        /// Date de départ réelle
        /// </summary>
        public DateTime? DateDepartReelle { get; set; }

        /// <summary>
        /// Date d'arrivée prévue
        /// </summary>
        public DateTime? DateArriveePrevue { get; set; }

        /// <summary>
        /// Date d'arrivée réelle
        /// </summary>
        public DateTime? DateArriveeReelle { get; set; }

        /// <summary>
        /// Date de clôture du dossier
        /// </summary>
        public DateTime? DateCloture { get; set; }

        /// <summary>
        /// Capacité totale en volume (m³)
        /// </summary>
        public decimal CapaciteVolume { get; set; }

        /// <summary>
        /// Capacité totale en poids (kg)
        /// </summary>
        public decimal CapacitePoids { get; set; }

        /// <summary>
        /// Nom du transporteur
        /// </summary>
        [StringLength(200)]
        public string? Transporteur { get; set; }

        /// <summary>
        /// Numéro de tracking du transporteur
        /// </summary>
        [StringLength(100)]
        public string? NumeroTracking { get; set; }

        /// <summary>
        /// Numéro du navire/vol
        /// </summary>
        [StringLength(100)]
        public string? NumeroNavireVol { get; set; }

        /// <summary>
        /// Documents douaniers (chemins séparés par des virgules)
        /// </summary>
        public string? DocumentsDouaniers { get; set; }

        /// <summary>
        /// Manifeste d'expédition (chemin du fichier)
        /// </summary>
        [StringLength(500)]
        public string? ManifesteExpedition { get; set; }

        /// <summary>
        /// Liste de colisage (chemin du fichier)
        /// </summary>
        [StringLength(500)]
        public string? ListeColisage { get; set; }

        /// <summary>
        /// Frais de transport
        /// </summary>
        public decimal FraisTransport { get; set; }

        /// <summary>
        /// Frais de dédouanement
        /// </summary>
        public decimal FraisDedouanement { get; set; }

        /// <summary>
        /// Autres frais
        /// </summary>
        public decimal AutresFrais { get; set; }

        /// <summary>
        /// Notes/commentaires
        /// </summary>
        public string? Commentaires { get; set; }

        // Navigation properties
        /// <summary>
        /// Liste des colis dans le conteneur
        /// </summary>
        public virtual ICollection<Colis> Colis { get; set; } = new List<Colis>();

        /// <summary>
        /// Constructeur
        /// </summary>
        public Conteneur()
        {
            DateOuverture = DateTime.UtcNow;
            NumeroDossier = GenerateNumeroDossier();
            Statut = StatutConteneur.Ouvert;
        }

        /// <summary>
        /// Génère un numéro de dossier unique
        /// </summary>
        private static string GenerateNumeroDossier()
        {
            var year = DateTime.Now.ToString("yyyy");
            var month = DateTime.Now.ToString("MM");
            var random = new Random().Next(1000, 9999);
            return $"CONT-{year}{month}-{random}";
        }

        /// <summary>
        /// Volume utilisé (somme des volumes des colis)
        /// </summary>
        public decimal VolumeUtilise => Colis?.Sum(c => c.Volume) ?? 0;

        /// <summary>
        /// Poids utilisé (somme des poids des colis)
        /// </summary>
        public decimal PoidsUtilise => Colis?.Sum(c => c.Poids) ?? 0;

        /// <summary>
        /// Taux de remplissage en volume (%)
        /// </summary>
        public decimal TauxRemplissageVolume => 
            CapaciteVolume > 0 ? Math.Round((VolumeUtilise / CapaciteVolume) * 100, 2) : 0;

        /// <summary>
        /// Taux de remplissage en poids (%)
        /// </summary>
        public decimal TauxRemplissagePoids => 
            CapacitePoids > 0 ? Math.Round((PoidsUtilise / CapacitePoids) * 100, 2) : 0;

        /// <summary>
        /// Nombre de clients dans le conteneur
        /// </summary>
        public int NombreClients => Colis?.Select(c => c.ClientId).Distinct().Count() ?? 0;

        /// <summary>
        /// Nombre total de colis
        /// </summary>
        public int NombreColis => Colis?.Count ?? 0;

        /// <summary>
        /// Coût total du conteneur
        /// </summary>
        public decimal CoutTotal => FraisTransport + FraisDedouanement + AutresFrais;

        /// <summary>
        /// Indique si le conteneur peut encore recevoir des colis
        /// </summary>
        public bool PeutRecevoirColis => 
            Statut == StatutConteneur.Ouvert && 
            TauxRemplissageVolume < 95 && 
            TauxRemplissagePoids < 95;
    }
}