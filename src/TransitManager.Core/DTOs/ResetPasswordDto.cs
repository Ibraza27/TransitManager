using System.ComponentModel.DataAnnotations;

namespace TransitManager.Core.DTOs
{
    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est requis.")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit faire au moins 6 caractères.")]
        public string NewPassword { get; set; } = string.Empty;

        [Compare(nameof(NewPassword), ErrorMessage = "Les mots de passe ne correspondent pas.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}