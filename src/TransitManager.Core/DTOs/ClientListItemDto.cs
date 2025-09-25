namespace TransitManager.Core.DTOs
{
    public class ClientListItemDto
    {
        public Guid Id { get; set; }
        public string CodeClient { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string TelephonePrincipal { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}