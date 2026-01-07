using System;

namespace TransitManager.Core.DTOs
{
    public class PortalAccessResult
    {
        public string Message { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}
