using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TransitManager.Core.DTOs
{
    public class ResendConfirmationDto
    {
        [Required]
        [JsonPropertyName("email")] // C'est la clé magique pour le JSON
        public string Email { get; set; } = string.Empty;
    }
}