using System;
using System.ComponentModel.DataAnnotations;

namespace TransitManager.Core.DTOs
{
    public class UserProfileDto
    {
        // --- Informations Client ---
        public Guid ClientId { get; set; }

        [Required(ErrorMessage = "Le nom est requis.")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis.")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est requis.")]
        [EmailAddress(ErrorMessage = "Format d'email invalide.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est requis.")]
        public string Telephone { get; set; } = string.Empty;

        public string? TelephoneSecondaire { get; set; }
        public string? Adresse { get; set; }
        public string? Ville { get; set; }
        public string? CodePostal { get; set; }
        public string? Pays { get; set; }

        // --- Gestion Mot de Passe (Optionnel pour la mise à jour) ---
        
        public string? AncienMotDePasse { get; set; }

        [MinLength(6, ErrorMessage = "Le mot de passe doit faire au moins 6 caractères.")]
        public string? NouveauMotDePasse { get; set; }

        [Compare(nameof(NouveauMotDePasse), ErrorMessage = "Les mots de passe ne correspondent pas.")]
        public string? ConfirmerMotDePasse { get; set; }
    }
}