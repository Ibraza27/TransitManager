using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
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

        public async Task<AuthenticationResult> LoginAsync(string identifier, string password)
        {
            try
            {
                // --- DÉBUT DE LA CORRECTION ---
                var lowerIdentifier = identifier.ToLower();
                var user = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => (u.Email.ToLower() == lowerIdentifier || u.NomUtilisateur.ToLower() == lowerIdentifier) && u.Actif);
                // --- FIN DE LA CORRECTION ---

                if (user == null)
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Identifiant ou mot de passe incorrect." };
                }

                if (user.EstVerrouille)
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Votre compte est temporairement verrouillé." };
                }

                if (!BCryptNet.Verify(password, user.MotDePasseHash))
                {
                    user.TentativesConnexionEchouees++;
                    if (user.TentativesConnexionEchouees >= 5)
                    {
                        user.DateVerrouillage = DateTime.UtcNow.AddMinutes(30);
                    }
                    await _context.SaveChangesAsync();
                    return new AuthenticationResult { Success = false, ErrorMessage = "Identifiant ou mot de passe incorrect." };
                }

                user.TentativesConnexionEchouees = 0;
                user.DateVerrouillage = null;
                user.DerniereConnexion = DateTime.UtcNow;

                if (user.Id != Guid.Empty)
                {
                    var auditLog = new AuditLog
                    {
                        UtilisateurId = user.Id,
                        Action = "LOGIN",
                        Entite = "Utilisateur",
                        EntiteId = user.Id.ToString(),
                        DateAction = DateTime.UtcNow
                    };
                    _context.AuditLogs.Add(auditLog);
                }
                
                await _context.SaveChangesAsync();
                _currentUser = user;
                return new AuthenticationResult { Success = true, User = user, RequiresPasswordChange = user.DoitChangerMotDePasse };
            }
            catch (Exception ex)
            {
                return new AuthenticationResult { Success = false, ErrorMessage = "Une erreur interne est survenue." };
            }
        }

        public async Task LogoutAsync()
        {
            if (_currentUser != null)
            {
                if (_currentUser.Id != Guid.Empty)
                {
                    var auditLog = new AuditLog
                    {
                        UtilisateurId = _currentUser.Id,
                        Action = "LOGOUT",
                        Entite = "Utilisateur",
                        EntiteId = _currentUser.Id.ToString(),
                        DateAction = DateTime.UtcNow
                    };
                    _context.AuditLogs.Add(auditLog);
                }
                await _context.SaveChangesAsync();
            }
            _currentUser = null;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            var user = await _context.Utilisateurs.FindAsync(userId);
            if (user == null || !BCryptNet.Verify(currentPassword, user.MotDePasseHash) || !IsPasswordValid(newPassword))
            {
                return false;
            }

            user.MotDePasseHash = BCryptNet.HashPassword(newPassword);
            user.DoitChangerMotDePasse = false;
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
    }
}