namespace TransitManager.Core.DTOs
{
    public class ManualNotificationDto
    {
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string PortalLink { get; set; } = string.Empty;
        
        // Company details
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;
        public string CompanyLogoUrl { get; set; } = string.Empty;
    }
}
