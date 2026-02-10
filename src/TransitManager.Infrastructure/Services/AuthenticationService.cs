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
using Microsoft.Extensions.Configuration;

namespace TransitManager.Infrastructure.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;
        private Utilisateur? _currentUser;
        public Utilisateur? CurrentUser => _currentUser;

        public AuthenticationService(
            IDbContextFactory<TransitContext> contextFactory,
            IEmailService emailService,
            IConfiguration configuration,
            INotificationService notificationService)
        {
            _contextFactory = contextFactory;
            _emailService = emailService;
            _configuration = configuration;
            _notificationService = notificationService;
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
				
                bool isStaff = user.Role == RoleUtilisateur.Administrateur || user.Role == RoleUtilisateur.Gestionnaire;

                if (!user.EmailConfirme && !isStaff)
                {
                    Console.WriteLine($"[5b] ÉCHEC : L'email {user.Email} n'est pas encore confirmé.");
                    return new AuthenticationResult 
                    { 
                        Success = false, 
                        ErrorMessage = "Email non confirmé.", // Message court pour le code
                        IsEmailUnconfirmed = true // <--- IMPORTANT
                    };
                }
				
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
                    Actif = true,
                    EmailConfirme = true // Auto-confirm email for inline client creation
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
                var token = Guid.NewGuid().ToString();

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
                    DateCreation = DateTime.UtcNow,
                    EmailConfirme = false, // Par défaut
                    TokenVerificationEmail = token,
                    DateExpirationTokenEmail = DateTime.UtcNow.AddDays(1)
                };

                context.Utilisateurs.Add(user);
                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                // ENVOI EMAIL CONFIRMATION
				string baseUrl = _configuration["ClientAppUrl"] ?? "https://localhost:7207";
				
				// CORRECTION : Utiliser Uri.EscapeDataString qui est plus strict que UrlEncode
				string encodedToken = Uri.EscapeDataString(user.TokenVerificationEmail);
				string encodedEmail = Uri.EscapeDataString(request.Email);
				
				string verifyLink = $"{baseUrl}/verify-email?email={encodedEmail}&token={encodedToken}";

                string message = $@"
                    <h3>Bienvenue sur TransitManager !</h3>
                    <p>Merci de confirmer votre adresse email en cliquant ici :</p>
                    <a href='{verifyLink}'>Confirmer mon compte</a>";

                // On lance l'envoi sans attendre pour ne pas bloquer l'UI (Fire and Forget ou background job idéalement)
                _ = _emailService.SendEmailAsync(request.Email, "Confirmation de compte", message);

                // NOTIF ADMIN
                await _notificationService.CreateAndSendAsync(
                    "👤 Nouveau Compte Client",
                    $"Nouveau client inscrit : {user.NomComplet} ({user.Email})",
                    null, // Admins
                    CategorieNotification.NouveauClient,
                    actionUrl: $"/clients/detail/{client.Id}",
                    relatedEntityId: client.Id,
                    relatedEntityType: "Client",
                    priorite: PrioriteNotification.Haute
                );

                return new AuthenticationResult { Success = true, User = user, ErrorMessage = "Compte créé. Veuillez vérifier vos emails." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"ERREUR INSCRIPTION: {ex.Message}");
                return new AuthenticationResult { Success = false, ErrorMessage = "Une erreur technique est survenue lors de l'inscription." };
            }
        }

        // --- GESTION DU MOT DE PASSE OUBLIÉ ---
        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false; // On ne dit pas si l'user existe ou non par sécurité, on retourne false silencieusement ou true générique

            // Générer token
            string token = Guid.NewGuid().ToString();
            user.TokenReinitialisation = token;
            user.ExpirationToken = DateTime.UtcNow.AddHours(1); // Valide 1h
            await context.SaveChangesAsync();

            // Générer Lien (Adapter l'URL de base selon votre déploiement Web)
            string baseUrl = _configuration["ClientAppUrl"] ?? "https://localhost:7207";
            string encodedToken = System.Net.WebUtility.UrlEncode(token);
            string encodedEmail = System.Net.WebUtility.UrlEncode(email);
            string resetLink = $"{baseUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

            string message = $@"
                <h3>Réinitialisation de mot de passe</h3>
                <p>Cliquez sur le lien ci-dessous pour définir un nouveau mot de passe :</p>
                <a href='{resetLink}'>Réinitialiser mon mot de passe</a>
                <p>Ce lien est valide pendant 1 heure.</p>";

            await _emailService.SendEmailAsync(email, "Réinitialisation mot de passe - TransitManager", message);
            return true;
        }

        public async Task<bool> ResetPasswordWithTokenAsync(string email, string token, string newPassword)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null
                || user.TokenReinitialisation != token
                || user.ExpirationToken < DateTime.UtcNow)
            {
                return false;
            }

            user.MotDePasseHash = BCryptNet.HashPassword(newPassword);
            user.TokenReinitialisation = null;
            user.ExpirationToken = null;
            user.DoitChangerMotDePasse = false; // Reset effectué

            // On déverrouille le compte si besoin
            user.DateVerrouillage = null;
            user.TentativesConnexionEchouees = 0;
            await context.SaveChangesAsync();
            return true;
        }

        // --- GESTION DE L'INSCRIPTION AVEC VÉRIFICATION ---
        public async Task<bool> VerifyEmailAsync(string email, string token)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null
                || user.TokenVerificationEmail != token
                || user.DateExpirationTokenEmail < DateTime.UtcNow)
            {
                return false;
            }

            user.EmailConfirme = true;
            user.TokenVerificationEmail = null;
            user.DateExpirationTokenEmail = null;

            await context.SaveChangesAsync();
            return true;
        }
		
		public async Task ResendConfirmationEmailAsync(string email)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);

            // Si l'utilisateur n'existe pas ou est déjà confirmé, on ne fait rien (sécurité)
            if (user == null || user.EmailConfirme) return;

            // On génère un nouveau token si l'ancien est expiré, sinon on renvoie le même
            if (string.IsNullOrEmpty(user.TokenVerificationEmail) || user.DateExpirationTokenEmail < DateTime.UtcNow)
            {
                user.TokenVerificationEmail = Guid.NewGuid().ToString();
                user.DateExpirationTokenEmail = DateTime.UtcNow.AddDays(1);
                await context.SaveChangesAsync();
            }

			string baseUrl = _configuration["ClientAppUrl"] ?? "https://localhost:7207";
            
            // CORRECTION : Utiliser Uri.EscapeDataString qui est plus strict que UrlEncode
            string encodedToken = Uri.EscapeDataString(user.TokenVerificationEmail);
            string encodedEmail = Uri.EscapeDataString(email);
            
            string verifyLink = $"{baseUrl}/verify-email?email={encodedEmail}&token={encodedToken}";

            string message = $@"
                <h3>Confirmation de compte</h3>
                <p>Vous avez demandé à recevoir de nouveau le lien de confirmation.</p>
                <p>Cliquez ici pour activer votre compte :</p>
                <a href='{verifyLink}'>Confirmer mon email</a>";

            await _emailService.SendEmailAsync(email, "Relance : Confirmation de compte", message);
        }
		
    }
}
