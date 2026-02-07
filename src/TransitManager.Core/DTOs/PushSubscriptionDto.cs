namespace TransitManager.Core.DTOs
{
    public class PushSubscriptionDto
    {
        public string Endpoint { get; set; } = null!;
        public string? P256dh { get; set; }
        public string? Auth { get; set; }
    }
}
