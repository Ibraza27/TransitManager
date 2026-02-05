using System;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    public class VehiculeListItemDto
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; } // AJOUT
        public string Immatriculation { get; set; } = string.Empty;
        public string Marque { get; set; } = string.Empty;
        public string Modele { get; set; } = string.Empty;
		public string? Commentaires { get; set; }
        
        // --- AJOUT : Champ manquant ---
        public int Annee { get; set; } 
        public bool HasMissingDocuments { get; set; } 
        
        public StatutVehicule Statut { get; set; }
        public string ClientNomComplet { get; set; } = string.Empty;
        public string? ClientTelephonePrincipal { get; set; }
        public string? ConteneurNumeroDossier { get; set; }
        public DateTime DateCreation { get; set; }
        public string DestinationFinale { get; set; } = string.Empty;

        // --- AJOUT : Champs financiers ---
        public decimal PrixTotal { get; set; }
        public decimal SommePayee { get; set; }
        
        // Propriété calculée
        public decimal RestantAPayer => PrixTotal - SommePayee;
    }
}