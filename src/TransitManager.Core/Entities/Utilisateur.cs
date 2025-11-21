using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Entities
{
    /// <summary>
    /// Représente un utilisateur du système
    /// </summary>
    public class Utilisateur : BaseEntity
    {
        // --- Champs privés ---
        private string _nomUtilisateur = string.Empty;
        private string _nom = string.Empty;
        private string _prenom = string.Empty;
        private string _email = string.Empty;
        private string? _telephone;
        private RoleUtilisateur _role;
        private Guid? _clientId;

        /// <summary>
        /// Nom d'utilisateur unique
        /// </summary>
        [Required]
        [StringLength(50)]
        public string NomUtilisateur 
        { 
            get => _nomUtilisateur; 
            set => SetProperty(ref _nomUtilisateur, value); 
        }

        /// <summary>
        /// Nom de famille
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Nom 
        { 
            get => _nom; 
            set => SetProperty(ref _nom, value); 
        }

        /// <summary>
        /// Prénom
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Prenom 
        { 
            get => _prenom; 
            set => SetProperty(ref _prenom, value); 
        }

        /// <summary>
        /// Adresse email
        /// </summary>
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email 
        { 
            get => _email; 
            set => SetProperty(ref _email, value); 
        }

        /// <summary>
        /// Mot de passe hashé
        /// </summary>
        [Required]
        public string MotDePasseHash { get; set; } = string.Empty;

        /// <summary>
        /// Sel pour le hashage du mot de passe
        /// </summary>
        public string? PasswordSalt { get; set; }

        /// <summary>
        /// Rôle de l'utilisateur
        /// </summary>
        public RoleUtilisateur Role 
        { 
            get => _role; 
            set => SetProperty(ref _role, value); // C'est ICI que la magie opère pour le champ grisé
        }

        /// <summary>
        /// Téléphone
        /// </summary>
        [StringLength(20)]
        public string? Telephone 
        { 
            get => _telephone; 
            set => SetProperty(ref _telephone, value); 
        }

        /// <summary>
        /// Photo de profil (chemin)
        /// </summary>
        [StringLength(500)]
        public string? PhotoProfil { get; set; }

        /// <summary>
        /// Date de dernière connexion
        /// </summary>
        public DateTime? DerniereConnexion { get; set; }

        /// <summary>
        /// Nombre de tentatives de connexion échouées
        /// </summary>
        public int TentativesConnexionEchouees { get; set; }

        /// <summary>
        /// Date de verrouillage du compte
        /// </summary>
        public DateTime? DateVerrouillage { get; set; }

        /// <summary>
        /// Indique si l'utilisateur doit changer son mot de passe
        /// </summary>
        public bool DoitChangerMotDePasse { get; set; }

        /// <summary>
        /// Token de réinitialisation du mot de passe
        /// </summary>
        public string? TokenReinitialisation { get; set; }

        /// <summary>
        /// Date d'expiration du token de réinitialisation
        /// </summary>
        public DateTime? ExpirationToken { get; set; }

        /// <summary>
        /// Préférences utilisateur (JSON)
        /// </summary>
        public string? Preferences { get; set; }

        /// <summary>
        /// Permissions spécifiques (JSON)
        /// </summary>
        public string? PermissionsSpecifiques { get; set; }

        /// <summary>
        /// Thème de l'interface (clair/sombre)
        /// </summary>
        [StringLength(20)]
        public string Theme { get; set; } = "Clair";

        /// <summary>
        /// Langue préférée
        /// </summary>
        [StringLength(10)]
        public string Langue { get; set; } = "fr-FR";

        /// <summary>
        /// Fuseau horaire
        /// </summary>
        [StringLength(50)]
        public string FuseauHoraire { get; set; } = "Europe/Paris";

        /// <summary>
        /// Indique si les notifications sont activées
        /// </summary>
        public bool NotificationsActivees { get; set; } = true;

        /// <summary>
        /// Indique si les notifications email sont activées
        /// </summary>
        public bool NotificationsEmail { get; set; } = true;

        /// <summary>
        /// Indique si les notifications SMS sont activées
        /// </summary>
        public bool NotificationsSMS { get; set; } = false;
		
        /// <summary>
        /// ID du client associé (si cet utilisateur est un compte client).
        /// Null pour les utilisateurs internes (admin, opérateur...).
        /// </summary>
        public Guid? ClientId
        {
            get => _clientId;
            set => SetProperty(ref _clientId, value);
        }

        /// <summary>
        /// Propriété de navigation vers l'entité Client associée.
        /// </summary>
        public virtual Client? Client { get; set; }

        /// <summary>
        /// Historique des actions (pour l'audit)
        /// </summary>
        public virtual ICollection<AuditLog> Audits { get; set; } = new List<AuditLog>();

        /// <summary>
        /// Constructeur
        /// </summary>
        public Utilisateur()
        {
            Role = RoleUtilisateur.Operateur;
        }

        /// <summary>
        /// Nom complet de l'utilisateur
        /// </summary>
        public string NomComplet => $"{Prenom} {Nom}";

        /// <summary>
        /// Indique si le compte est verrouillé
        /// </summary>
        public bool EstVerrouille => DateVerrouillage.HasValue && DateVerrouillage.Value > DateTime.UtcNow;

        /// <summary>
        /// Vérifie si l'utilisateur a une permission spécifique
        /// </summary>
        public bool APermission(string permission)
        {
            // Administrateurs ont toutes les permissions
            if (Role == RoleUtilisateur.Administrateur)
                return true;

            // Vérifier les permissions du rôle
            var permissionsRole = GetPermissionsParRole(Role);
            if (permissionsRole.Contains(permission))
                return true;

            // Vérifier les permissions spécifiques
            // TODO: Implémenter la désérialisation JSON des permissions spécifiques
            return false;
        }

        /// <summary>
        /// Obtient les permissions par défaut selon le rôle
        /// </summary>
        private static List<string> GetPermissionsParRole(RoleUtilisateur role)
        {
            return role switch
            {
                RoleUtilisateur.Administrateur => new List<string> { "*" }, // Toutes les permissions
                RoleUtilisateur.Gestionnaire => new List<string> 
                { 
                    "clients.*", "colis.*", "conteneurs.*", "paiements.voir", 
                    "rapports.*", "documents.*" 
                },
                RoleUtilisateur.Operateur => new List<string> 
                { 
                    "clients.voir", "clients.creer", "colis.*", 
                    "conteneurs.voir", "documents.voir" 
                },
                RoleUtilisateur.Comptable => new List<string> 
                { 
                    "clients.voir", "paiements.*", "factures.*", 
                    "rapports.financiers", "documents.financiers" 
                },
                RoleUtilisateur.Invite => new List<string> { "*.voir" }, // Lecture seule
                _ => new List<string>()
            };
        }
    }

}