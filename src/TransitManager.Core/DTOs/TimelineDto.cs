using System;

namespace TransitManager.Core.DTOs
{
    public class TimelineDto
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Statut { get; set; }
        
        // Icône suggérée pour l'UI (ex: "box", "truck", "check")
        public string IconKey { get; set; } = "circle"; 
        public string ColorHex { get; set; } = "#808080";
    }
}