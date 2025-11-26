using System;
using System.Collections.Generic;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class ConteneurDetailDto
    {
        // Infos Conteneur
        public Guid Id { get; set; }
        public string NumeroDossier { get; set; } = string.Empty;
        public string? NumeroPlomb { get; set; }
        public string? NomCompagnie { get; set; }
        public string? NomTransitaire { get; set; }
        public string Destination { get; set; } = string.Empty;
        public string PaysDestination { get; set; } = string.Empty;
        public StatutConteneur Statut { get; set; }
        public string? Commentaires { get; set; }

        // Dates
        public DateTime? DateReception { get; set; }
        public DateTime? DateChargement { get; set; }
        public DateTime? DateDepart { get; set; }
        public DateTime? DateArriveeDestination { get; set; }
        public DateTime? DateDedouanement { get; set; }
        public DateTime? DateCloture { get; set; }

        // Contenu (Utilisation des ListItemDto existants pour la cohérence)
        public List<ColisListItemDto> Colis { get; set; } = new();
        public List<VehiculeListItemDto> Vehicules { get; set; } = new();

        // Stats globales (Calculées côté serveur)
        public decimal PrixTotalGlobal { get; set; }
        public decimal TotalPayeGlobal { get; set; }
        public decimal TotalRestantGlobal => PrixTotalGlobal - TotalPayeGlobal;

        // Stats par Client
        public List<ClientConteneurStatDto> StatsParClient { get; set; } = new();
    }

    public class ClientConteneurStatDto
    {
        public Guid ClientId { get; set; }
        public string NomClient { get; set; } = string.Empty;
		public string? Telephone { get; set; }
        public int NombreColis { get; set; }
        public int NombreVehicules { get; set; }
        public decimal TotalPrix { get; set; }
        public decimal TotalPaye { get; set; }
        public decimal ResteAPayer => TotalPrix - TotalPaye;
    }
}