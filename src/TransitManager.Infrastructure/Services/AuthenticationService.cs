using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using BCryptNet = BCrypt.Net.BCrypt;

namespace TransitManager.Infrastructure.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private Utilisateur? _currentUser;

        public Utilisateur? CurrentUser => _currentUser;

        public AuthenticationService(IDbContextFactory<TransitContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<AuthenticationResult> LoginAsync(string identifier, string password)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            try
            {
                // On recherche l'utilisateur soit par son email, soit par son nom d'utilisateur
                var user = await context.Utilisateurs
                    .FirstOrDefaultAsync(u => (u.Email == identifier || u.NomUtilisateur == identifier) && u.Actif);

                if (user == null)
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Identifiant ou mot de passe incorrect." };
                }

                if (user.EstVerrouille)
                {
                    return new AuthenticationResult { Success = false, ErrorMessage = "Votre compte est temporairement verrouillé." };
                }

                if (!BCrypt.Net.BCrypt.Verify(password, user.MotDePasseHash))
                {
                    user.TentativesConnexionEchouees++;
                    if (user.TentativesConnexionEchouees >= 5)
                    {
                        user.DateVerrouillage = DateTime.UtcNow.AddMinutes(30);
                    }
                    await context.SaveChangesAsync();
                    return new AuthenticationResult { Success = false, ErrorMessage = "Identifiant ou mot de passe incorrect." };
                }

                user.TentativesConnexionEchouees = 0;
                user.DateVerrouillage = null;
                user.DerniereConnexion = DateTime.UtcNow;


				// Vérification que l'utilisateur est valide avant de créer le log
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
					context.AuditLogs.Add(auditLog);
				}
                
                await context.SaveChangesAsync();
                _currentUser = user;
                return new AuthenticationResult { Success = true, User = user, RequiresPasswordChange = user.DoitChangerMotDePasse };
            }
            catch (Exception ex)
            {
                // Log l'exception ici
                return new AuthenticationResult { Success = false, ErrorMessage = $"Une erreur interne est survenue." };
            }
        }

        public async Task LogoutAsync()
        {
            if (_currentUser != null)
            {
                await using var context = await _contextFactory.CreateDbContextAsync();

				// Vérification que l'utilisateur actuel est valide
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
					context.AuditLogs.Add(auditLog);
				}
                await context.SaveChangesAsync();
            }
            _currentUser = null;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FindAsync(userId);
            if (user == null || !BCryptNet.Verify(currentPassword, user.MotDePasseHash) || !IsPasswordValid(newPassword))
            {
                return false;
            }

            user.MotDePasseHash = BCryptNet.HashPassword(newPassword);
            user.DoitChangerMotDePasse = false;
            await context.SaveChangesAsync();
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