using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using BCryptNet = BCrypt.Net.BCrypt;
using TransitManager.Core.Enums;
using TransitManager.Core.DTOs;

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
            Console.WriteLine("--- DÉBUT AUTHENTIFICATION ---");
            Console.WriteLine($"[1] Identifiant reçu : {identifier}");
            try
            {
                var lowerIdentifier = identifier.ToLower();
                Console.WriteLine($"[2] Identifiant normalisé : {lowerIdentifier}");
                // On sépare la logique pour être sûr.
                Console.WriteLine("[3] Recherche de l'utilisateur par Email...");
                await using var context = await _contextFactory.CreateDbContextAsync();
                var user = await context.Utilisateurs
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == lowerIdentifier && u.Actif);
                if (user == null)
                {
                    Console.WriteLine("[3a] Utilisateur non trouvé par Email. Recherche par NomUtilisateur...");
                    user = await context.Utilisateurs
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
                    // MODIFICATION ICI : On renvoie la date de fin
                    return new AuthenticationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Votre compte est temporairement verrouillé.",
                        LockoutEnd = user.DateVerrouillage 
                    };
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
                    await context.SaveChangesAsync();
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
                    context.AuditLogs.Add(auditLog);
                }
                Console.WriteLine("[7] Authentification réussie. Enregistrement des modifications et retour.");
                await context.SaveChangesAsync();
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
                await using var context = await _contextFactory.CreateDbContextAsync();
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

        public async Task<(Utilisateur? User, string? TemporaryPassword)> CreateOrResetPortalAccessAsync(Guid clientId)
        {
            Console.WriteLine($"🔑 [AuthService] Tentative de création/réinitialisation de l'accès pour le client ID: {clientId}");
            await using var context = await _contextFactory.CreateDbContextAsync();
            var client = await context.Clients.FindAsync(clientId);
            if (client == null)
            {
                Console.WriteLine("🔑 [AuthService] ❌ Client non trouvé.");
                throw new InvalidOperationException("Le client spécifié n'existe pas.");
            }
            // Chercher un utilisateur déjà lié à ce client
            var user = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == clientId);

            string temporaryPassword = GenerateTemporaryPassword();
            string passwordHash = BCryptNet.HashPassword(temporaryPassword);
            if (user == null)
            {
                // --- Création de l'utilisateur ---
                Console.WriteLine("🔑 [AuthService] ℹ️ Aucun utilisateur existant. Création d'un nouveau compte client.");

                string username = await GenerateUniqueUsernameAsync(client.Prenom, client.Nom, context);

                user = new Utilisateur
                {
                    NomUtilisateur = username,
                    Email = client.Email ?? $"{username}@default.com", // Utilise un email par défaut si non fourni
                    Nom = client.Nom,
                    Prenom = client.Prenom,
                    Telephone = client.TelephonePrincipal,
                    MotDePasseHash = passwordHash,
                    Role = RoleUtilisateur.Client,
                    ClientId = client.Id,
                    Actif = true
                };

                context.Utilisateurs.Add(user);
                Console.WriteLine($"🔑 [AuthService] ✅ Nouvel utilisateur créé. Nom d'utilisateur: {username}");
            }
            else
            {
                // --- Réinitialisation du mot de passe ---
                Console.WriteLine($"🔑 [AuthService] ℹ️ Utilisateur existant trouvé ({user.NomUtilisateur}). Réinitialisation du mot de passe.");
                user.MotDePasseHash = passwordHash;
                user.Actif = true; // S'assurer que le compte est réactivé si besoin
                context.Utilisateurs.Update(user);
            }
            try
            {
                await context.SaveChangesAsync();
                Console.WriteLine("🔑 [AuthService] ✅ Données utilisateur sauvegardées dans la base.");
                return (user, temporaryPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔑 [AuthService] 💥 Erreur lors de la sauvegarde : {ex.Message}");
                return (null, null);
            }
        }

        public async Task SynchronizeClientDataAsync(Client client)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == client.Id);
            if (user != null)
            {
                Console.WriteLine($"🔄 [AuthService] Synchronisation des données pour l'utilisateur lié au client {client.Id}");
                user.Nom = client.Nom;
                user.Prenom = client.Prenom;
                user.Email = client.Email ?? user.Email;
                user.Telephone = client.TelephonePrincipal;
                context.Utilisateurs.Update(user);
                await context.SaveChangesAsync();
                Console.WriteLine("🔄 [AuthService] ✅ Synchronisation terminée.");
            }
        }

        // --- Méthodes privées utilitaires ---
        private async Task<string> GenerateUniqueUsernameAsync(string prenom, string nom, TransitContext context)
        {
            var baseUsername = $"{prenom.FirstOrDefault()}".ToLower() + nom.ToLower().Replace(" ", "");
            if (baseUsername.Length > 20) baseUsername = baseUsername.Substring(0, 20);
            var finalUsername = baseUsername;
            int counter = 1;
            while (await context.Utilisateurs.AnyAsync(u => u.NomUtilisateur == finalUsername))
            {
                counter++;
                finalUsername = $"{baseUsername}{counter}";
            }
            return finalUsername;
        }

        private string GenerateTemporaryPassword(int length = 10)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            var sb = new StringBuilder();
            var rand = new Random();
            while (0 < length--)
            {
                sb.Append(validChars[rand.Next(validChars.Length)]);
            }
            return sb.ToString();
        }
		
		public async Task<AuthenticationResult> RegisterClientAsync(RegisterClientRequestDto request)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // 1. Vérification unicité Email
            var emailExists = await context.Utilisateurs.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
            if (emailExists)
            {
                return new AuthenticationResult { Success = false, ErrorMessage = "Cet email est déjà utilisé. Veuillez contacter le support si vous avez oublié vos accès." };
            }

            // 2. Transaction Atomique
            using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                // A. Création du Client
                var client = new Client
                {
                    Nom = request.Nom,
                    Prenom = request.Prenom,
                    Email = request.Email,
                    TelephonePrincipal = request.Telephone,
                    TelephoneSecondaire = request.TelephoneSecondaire,
                    AdressePrincipale = request.Adresse,
                    CodePostal = request.CodePostal,
                    Ville = request.Ville,
                    Pays = request.Pays ?? "France",
                    Actif = true,
                    DateInscription = DateTime.UtcNow
                };
                
                context.Clients.Add(client);
                await context.SaveChangesAsync(); // Pour générer l'ID du client

                // B. Création de l'Utilisateur lié
                var user = new Utilisateur
                {
                    NomUtilisateur = request.Email, // On utilise l'email comme login
                    Email = request.Email,
                    Nom = request.Nom,
                    Prenom = request.Prenom,
                    Telephone = request.Telephone,
                    MotDePasseHash = BCryptNet.HashPassword(request.Password),
                    Role = RoleUtilisateur.Client,
                    ClientId = client.Id, // Liaison automatique
                    Actif = true,
                    DateCreation = DateTime.UtcNow
                };

                context.Utilisateurs.Add(user);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new AuthenticationResult { Success = true, User = user };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"ERREUR INSCRIPTION: {ex.Message}");
                return new AuthenticationResult { Success = false, ErrorMessage = "Une erreur technique est survenue lors de l'inscription." };
            }
        }
		
    }
}
