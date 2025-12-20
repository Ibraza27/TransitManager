using System;

namespace TransitManager.Core.DTOs
{
    public class DashboardEntityDto
    {
        public Guid Id { get; set; }
        public string? Type { get; set; } // "Colis" or "Vehicule"
        public string? Reference { get; set; }
        public string? Description { get; set; }
        public string? ClientName { get; set; }
        public DateTime DateCreation { get; set; }
        public int DaysDelay { get; set; }
        public string? Status { get; set; }
    }
}
