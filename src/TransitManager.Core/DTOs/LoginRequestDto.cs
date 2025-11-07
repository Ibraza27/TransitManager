using System.Text.Json.Serialization; // AJOUTER CE USING

namespace TransitManager.Core.DTOs
{
    public class LoginRequestDto
    {
        private string _email = string.Empty;
        private string _password = string.Empty;

        [JsonPropertyName("email")] // On spécifie explicitement le nom du champ JSON
        public string Email
        {
            get => _email;
            set => _email = value ?? string.Empty;
        }

        [JsonPropertyName("password")] // On spécifie explicitement le nom du champ JSON
        public string Password
        {
            get => _password;
            set => _password = value ?? string.Empty;
        }
    }
}