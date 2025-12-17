
using System;
using System.Collections.Generic;

namespace TransitManager.Core.DTOs
{
    public class AdminDashboardStatsDto
    {
        // KPIs
        public int ColisEnTransit { get; set; }
        public int DocsAValider { get; set; }
        public int NouveauxClientsMois { get; set; }
        public decimal VolumeMensuel { get; set; } // En m3 ou unité pertinente

        // Charts Data
        public List<MonthlyMetricDto> RevenueLast6Months { get; set; } = new();
        public List<MonthlyMetricDto> VolumeLast6Months { get; set; } = new();
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
    }

    public class MonthlyMetricDto
    {
        public string Month { get; set; } = string.Empty; // e.g., "Jan", "Feb"
        public decimal Value { get; set; }
    }
}
