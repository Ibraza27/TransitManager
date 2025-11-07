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
            Console.WriteLine("--- DÉBUT AUTHENTIFICATION ---");
            Console.WriteLine($"[1] Identifiant reçu : {identifier}");
            try
            {
                var lowerIdentifier = identifier.ToLower();
                Console.WriteLine($"[2] Identifiant normalisé : {lowerIdentifier}");
                // On sépare la logique pour être sûr.
                Console.WriteLine("[3] Recherche de l'utilisateur par Email...");
                var user = await _context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == lowerIdentifier && u.Actif);
                if (user == null)
                {
                    Console.WriteLine("[3a] Utilisateur non trouvé par Email. Recherche par NomUtilisateur...");
                    user = await _context.Utilisateurs
                        .FirstOrDefaultAsync(u => u.NomUtilisateur.ToLower() == lowerIdentifier && u.Actif);
                }
                if (user == null)
                {
                    Console.WriteLine("[4] ÉCHEC : Utilisateur non trouvé dans la base de données après les deux recherches.");
                    return new AuthenticationResult { Success = false, ErrorMessage = "Identifiant ou mot de passe incorrect (Utilisateur Inconnu)." };
                }
                Console.WriteLine($"[4] SUCCÈS : Utilisateur trouvé. ID : {user.Id}, Nom : {user.NomComplet}");
                if (user.EstVerrouille)
                {
                    Console.WriteLine("[5] ÉCHEC : Le compte est verrouillé.");
                    return new AuthenticationResult { Success = false, ErrorMessage = "Votre compte est temporairement verrouillé." };
                }
                Console.WriteLine("[5] Compte non verrouillé. Vérification du mot de passe...");

                bool isPasswordValid = BCryptNet.Verify(password, user.MotDePasseHash);
                if (!isPasswordValid)
                {
                    Console.WriteLine("[6] ÉCHEC : La vérification BCrypt du mot de passe a échoué.");
                    user.TentativesConnexionEchouees++;
                    if (user.TentativesConnexionEchouees >= 5)
                    {
                        user.DateVerrouillage = DateTime.UtcNow.AddMinutes(30);
                    }
                    await _context.SaveChangesAsync();
                    return new AuthenticationResult { Success = false, ErrorMessage = "Identifiant ou mot de passe incorrect (Mot de passe invalide)." };
                }
                Console.WriteLine("[6] SUCCÈS : Mot de passe valide.");
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

                Console.WriteLine("[7] Authentification réussie. Enregistrement des modifications et retour.");
                await _context.SaveChangesAsync();
                _currentUser = user;
                return new AuthenticationResult { Success = true, User = user, RequiresPasswordChange = user.DoitChangerMotDePasse };
            }
            catch (Exception ex)
            {
                Console.WriteLine("--- ERREUR CRITIQUE DANS LoginAsync ---");
                Console.WriteLine($"[E1] Type d'exception : {ex.GetType().Name}");
                Console.WriteLine($"[E2] Message : {ex.Message}");
                Console.WriteLine($"[E3] StackTrace : {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"--- EXCEPTION INTERNE ---");
                    Console.WriteLine($"[E4] Type : {ex.InnerException.GetType().Name}");
                    Console.WriteLine($"[E5] Message : {ex.InnerException.Message}");
                }
                return new AuthenticationResult { Success = false, ErrorMessage = $"Une erreur interne est survenue : {ex.Message}" };
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
