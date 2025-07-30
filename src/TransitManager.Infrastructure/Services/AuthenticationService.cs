using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using BCryptNet = BCrypt.Net.BCrypt;

namespace TransitManager.Infrastructure.Services
{


    public class AuthenticationService : IAuthenticationService
    {
        private readonly TransitContext _context;
        private Utilisateur? _currentUser;

        public Utilisateur? CurrentUser => _currentUser;

        public AuthenticationService(TransitContext context)
        {
            _context = context;
        }

        public async Task<AuthenticationResult> LoginAsync(string username, string password)
        {
            try
            {
                // Rechercher l'utilisateur
                var user = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.NomUtilisateur == username && u.Actif);

                if (user == null)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Nom d'utilisateur ou mot de passe incorrect."
                    };
                }

                // Vérifier si le compte est verrouillé
                if (user.EstVerrouille)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Votre compte est temporairement verrouillé. Veuillez réessayer plus tard."
                    };
                }

                // Vérifier le mot de passe
                if (!BCryptNet.Verify(password, user.MotDePasseHash))
                {
                    // Incrémenter les tentatives échouées
                    user.TentativesConnexionEchouees++;
                    
                    // Verrouiller le compte après 5 tentatives
                    if (user.TentativesConnexionEchouees >= 5)
                    {
                        user.DateVerrouillage = DateTime.UtcNow.AddMinutes(30);
                    }

                    await _context.SaveChangesAsync();

                    return new AuthenticationResult
                    {
                        Success = false,
                        ErrorMessage = "Nom d'utilisateur ou mot de passe incorrect."
                    };
                }

                // Connexion réussie
                user.TentativesConnexionEchouees = 0;
                user.DateVerrouillage = null;
                user.DerniereConnexion = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _currentUser = user;

                // Créer un log d'audit
                var auditLog = new AuditLog
                {
                    UtilisateurId = user.Id,
                    Action = "LOGIN",
                    Entite = "Utilisateur",
                    EntiteId = user.Id.ToString(),
                    DateAction = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();

                return new AuthenticationResult
                {
                    Success = true,
                    User = user,
                    RequiresPasswordChange = user.DoitChangerMotDePasse
                };
            }
            catch (Exception ex)
            {
                return new AuthenticationResult
                {
                    Success = false,
                    ErrorMessage = $"Une erreur s'est produite lors de la connexion : {ex.Message}"
                };
            }
        }

        public async Task LogoutAsync()
        {
            if (_currentUser != null)
            {
                // Créer un log d'audit
                var auditLog = new AuditLog
                {
                    UtilisateurId = _currentUser.Id,
                    Action = "LOGOUT",
                    Entite = "Utilisateur",
                    EntiteId = _currentUser.Id.ToString(),
                    DateAction = DateTime.UtcNow
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }

            _currentUser = null;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _context.Utilisateurs.FindAsync(userId);
            if (user == null) return false;

            // Vérifier le mot de passe actuel
            if (!BCryptNet.Verify(currentPassword, user.MotDePasseHash))
                return false;

            // Valider le nouveau mot de passe
            if (!IsPasswordValid(newPassword))
                return false;

            // Hasher et enregistrer le nouveau mot de passe
            user.MotDePasseHash = BCryptNet.HashPassword(newPassword);
            user.DoitChangerMotDePasse = false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResetPasswordAsync(string email)
        {
            var user = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email == email && u.Actif);

            if (user == null) return false;

            // Générer un token de réinitialisation
            user.TokenReinitialisation = GenerateResetToken();
            user.ExpirationToken = DateTime.UtcNow.AddHours(24);

            await _context.SaveChangesAsync();

            // TODO: Envoyer l'email avec le token
            // await _emailService.SendPasswordResetEmailAsync(user.Email, user.TokenReinitialisation);

            return true;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var user = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.TokenReinitialisation == token && 
                                         u.ExpirationToken > DateTime.UtcNow);

            return user != null;
        }

        public async Task<bool> SetNewPasswordAsync(string token, string newPassword)
        {
            var user = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.TokenReinitialisation == token && 
                                         u.ExpirationToken > DateTime.UtcNow);

            if (user == null) return false;

            // Valider le nouveau mot de passe
            if (!IsPasswordValid(newPassword))
                return false;

            // Hasher et enregistrer le nouveau mot de passe
            user.MotDePasseHash = BCryptNet.HashPassword(newPassword);
            user.TokenReinitialisation = null;
            user.ExpirationToken = null;
            user.DoitChangerMotDePasse = false;

            await _context.SaveChangesAsync();
            return true;
        }

        public bool HasPermission(string permission)
        {
            return _currentUser?.APermission(permission) ?? false;
        }

        public async Task<bool> UpdateLastLoginAsync(Guid userId)
        {
            var user = await _context.Utilisateurs.FindAsync(userId);
            if (user == null) return false;

            user.DerniereConnexion = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private bool IsPasswordValid(string password)
        {
            if (password.Length < 8) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;
            return true;
        }

        private string GenerateResetToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "");
        }
    }

}