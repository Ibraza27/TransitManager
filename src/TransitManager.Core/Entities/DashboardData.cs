using System.Collections.Generic;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    public class DashboardData
    {
        public int TotalClients { get; set; }
        public int NouveauxClients { get; set; }
        public int ColisEnAttente { get; set; }
        public int ColisEnTransit { get; set; }
        public int ConteneursActifs { get; set; }
        public decimal ChiffreAffaireMois { get; set; }
        public decimal PaiementsEnAttente { get; set; }
        public Dictionary<string, decimal>? RevenueByMonth { get; set; }
        public Dictionary<StatutColis, int>? ColisByStatus { get; set; }
    }
}