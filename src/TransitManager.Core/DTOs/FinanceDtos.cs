using System;
using System.Collections.Generic;
using TransitManager.Core.Enums;

namespace TransitManager.Core.DTOs
{
    // --- ADMIN STATS ---
    public class FinanceStatsDto
    {
        public decimal ChiffreAffairesMensuel { get; set; }
        public decimal ChiffreAffairesAnnuel { get; set; }
        public decimal TotalEncaisse { get; set; }
        public decimal TotalRestantDu { get; set; }
        public int NombrePaiementsRetard { get; set; }
        
        // Pour les graphiques (12 derniers mois)
        public List<MonthlyRevenueDto> RevenueChartData { get; set; } = new();
    }

    public class MonthlyRevenueDto
    {
        public string MonthLabel { get; set; } = string.Empty; // ex: "Jan 2024"
        public decimal Revenue { get; set; }
    }

    // --- TRANSACTIONS LIST (Shared) ---
    public class FinancialTransactionDto
    {
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string ReferenceRecu { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty; // "Colis", "Véhicule", "Conteneur"
        public string EntityReference { get; set; } = string.Empty; // Ref Colis/Vehicule
        public decimal Montant { get; set; }
        public string ModePaiement { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public Guid? EntityId { get; set; } // Pour le lien direct
    }

    // --- CLIENT VIEW ---
    public class ClientFinanceSummaryDto
    {
        public decimal SoldeTotalAPayer { get; set; }
        public decimal RestantAPayerColis { get; set; } // NEW
        public decimal RestantAPayerVehicule { get; set; } // NEW
        public decimal TotalPayeHistorique { get; set; }
        public List<UnpaidItemDto> Impayes { get; set; } = new();
        public List<FinancialTransactionDto> DerniersPaiements { get; set; } = new();
    }

    public class UnpaidItemDto
    {
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty; // "Colis", "Vehicule"
        public string Reference { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MontantTotal { get; set; }
        public decimal RestantAPayer { get; set; }
        public DateTime DateCreation { get; set; }
    }
}
