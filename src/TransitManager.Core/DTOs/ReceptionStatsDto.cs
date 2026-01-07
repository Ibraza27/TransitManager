using System;

namespace TransitManager.Core.DTOs
{
    public class ReceptionStatsDto
    {
        public int TotalControls { get; set; }
        public double AverageService { get; set; }
        public double AverageCondition { get; set; }
        public double AverageCommunication { get; set; }
        public double AverageRecommendation { get; set; }
        
        // Distributions
        public int CountFull { get; set; }
        public int CountPartial { get; set; }
        public int CountDamaged { get; set; }
        public Dictionary<string, int> MonthlyCount { get; set; } = new();
        public int ControlsWithIssues { get; set; }
        public double IssuePercentage { get; set; }
        public double AverageRating { get; set; } // Aggregated average
    }
}
