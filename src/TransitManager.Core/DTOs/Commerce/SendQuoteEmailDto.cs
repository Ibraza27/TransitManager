namespace TransitManager.Core.DTOs.Commerce
{
    public class SendQuoteEmailDto
    {
        public string? Recipients { get; set; } // Comma-separated
        public string? Cc { get; set; }
        public string? Bcc { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public bool CopyToSender { get; set; } = false;
        public List<Guid>? TempAttachmentIds { get; set; }
    }
}
