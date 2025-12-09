using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization; // <--- Indispensable

namespace TransitManager.Core.DTOs
{
    public class VerifyEmailDto
    {
        [Required]
        [EmailAddress]
        [JsonPropertyName("email")] // Force la minuscule
        public string Email { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("token")] // Force la minuscule
        public string Token { get; set; } = string.Empty;
    }
}