namespace TransitManager.Core.DTOs
{
    /// <summary>
    /// DTO envoyé par le navigateur lors de l'abonnement aux notifications push
    /// </summary>
    public class PushSubscriptionDto
    {
        public string Endpoint { get; set; } = string.Empty;
        public PushSubscriptionKeysDto Keys { get; set; } = new();
    }

    public class PushSubscriptionKeysDto
    {
        public string P256dh { get; set; } = string.Empty;
        public string Auth { get; set; } = string.Empty;
    }
}
