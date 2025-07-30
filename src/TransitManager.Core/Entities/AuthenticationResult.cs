namespace TransitManager.Core.Entities
{
    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Utilisateur? User { get; set; }
        public bool RequiresPasswordChange { get; set; }
    }
}