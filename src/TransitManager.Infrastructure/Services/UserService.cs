// src/TransitManager.Infrastructure/Services/UserService.cs

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Exceptions;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using BCryptNet = BCrypt.Net.BCrypt;

namespace TransitManager.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly IAuthenticationService _authenticationService;
        private readonly INotificationService _notificationService;

        public UserService(
            IDbContextFactory<TransitContext> contextFactory, 
            IAuthenticationService authenticationService,
            INotificationService notificationService)
        {
            _contextFactory = contextFactory;
            _authenticationService = authenticationService;
            _notificationService = notificationService;
        }

        public async Task<Utilisateur?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            // On inclut le client lié pour l'affichage
            return await context.Utilisateurs
                .Include(u => u.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<IEnumerable<Utilisateur>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Utilisateurs
                .Include(u => u.Client) // Inclure les infos du client si lié
                .AsNoTracking()
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToListAsync();
        }

        public async Task<Utilisateur> CreateAsync(Utilisateur user, string password)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // Valider que le nom d'utilisateur et l'email sont uniques
            if (await context.Utilisateurs.AnyAsync(u => u.NomUtilisateur == user.NomUtilisateur))
                throw new InvalidOperationException("Ce nom d'utilisateur est déjà pris.");
            if (await context.Utilisateurs.AnyAsync(u => u.Email == user.Email))
                throw new InvalidOperationException("Cette adresse email est déjà utilisée.");
                
            user.MotDePasseHash = BCryptNet.HashPassword(password);
            
			// Si on lie un client, on synchronise les données
			if (user.ClientId.HasValue)
			{
				var client = await context.Clients.FindAsync(user.ClientId.Value); // Ici, EF charge le Client (Instance A)
				if (client != null)
				{
					user.Nom = client.Nom;
					user.Prenom = client.Prenom;
					user.Email = client.Email ?? user.Email;
					user.Telephone = client.TelephonePrincipal;
				}
			}
			
			// --- CORRECTION CRUCIALE ---
			// On s'assure que la propriété de navigation est nulle pour éviter 
			// qu'EF n'essaie d'attacher l'objet Client venant de l'UI (Instance B)
			// alors qu'il connait déjà Instance A.
			user.Client = null; 
			// ---------------------------
            
            await context.Utilisateurs.AddAsync(user);
            await context.SaveChangesAsync();

            // NOTIF ADMIN
            if (user.Role == RoleUtilisateur.Client)
            {
                await _notificationService.CreateAndSendAsync(
                    "👤 Nouveau Compte Client",
                    $"Compte crée pour : {user.NomComplet} ({user.Email})",
                    null, // Admins
                    CategorieNotification.NouveauClient,
                    actionUrl: user.ClientId.HasValue ? $"/clients/detail/{user.ClientId}" : "/users",
                    relatedEntityId: user.ClientId,
                    relatedEntityType: "Client"
                );
            }

            return user;
        }

		public async Task<Utilisateur> UpdateAsync(Utilisateur userFromUI) // Renommer le paramètre pour plus de clarté
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			
			// === DÉBUT DE LA CORRECTION ===
			
			// 1. Récupérer l'entité existante de la base de données, SANS AsNoTracking()
			var userInDb = await context.Utilisateurs.FirstOrDefaultAsync(u => u.Id == userFromUI.Id);
			if (userInDb == null)
			{
				throw new InvalidOperationException("L'utilisateur que vous essayez de modifier a été supprimé.");
			}

			// 2. Vérifier manuellement la concurrence
			if (userFromUI.RowVersion != null && !userInDb.RowVersion.SequenceEqual(userFromUI.RowVersion))
			{
				throw new DbUpdateConcurrencyException("Les données ont été modifiées par un autre utilisateur.");
			}
			
			userFromUI.Client = null; // On casse la référence objet, on garde juste l'ID

			// 3. Copier les valeurs modifiées de l'UI vers l'entité suivie par le DbContext
			context.Entry(userInDb).CurrentValues.SetValues(userFromUI);

			// Si on lie un client, on synchronise les données
			if (userInDb.ClientId.HasValue)
			{
				var client = await context.Clients.FindAsync(userInDb.ClientId.Value);
				if (client != null)
				{
					// Note: Le service d'authentification a besoin d'être adapté pour utiliser la DbContextFactory
					// Pour l'instant, on fait la synchro manuellement ici.
					userInDb.Nom = client.Nom;
					userInDb.Prenom = client.Prenom;
					userInDb.Email = client.Email ?? userInDb.Email;
					userInDb.Telephone = client.TelephonePrincipal;
				}
			}

			try
			{
				await context.SaveChangesAsync();
				return userInDb; // Retourner l'entité mise à jour
			}
			catch (DbUpdateConcurrencyException)
			{
				// Cette exception peut encore se produire si les données changent EXACTEMENT
				// entre notre chargement et SaveChangesAsync(). On la relance pour que l'UI la gère.
				throw new ConcurrencyException("Ce compte a été modifié par un autre utilisateur. Vos modifications n'ont pas pu être enregistrées.");
			}
			// === FIN DE LA CORRECTION ===
		}
        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FindAsync(id);
            if (user == null) return false;
            
            // Suppression logique
            user.Actif = false;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<string?> ResetPasswordAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FindAsync(id);
            if (user == null) return null;

            string temporaryPassword = GenerateTemporaryPassword();
            user.MotDePasseHash = BCryptNet.HashPassword(temporaryPassword);
            user.DoitChangerMotDePasse = true; // Forcer le changement au prochain login

            await context.SaveChangesAsync();
            return temporaryPassword;
        }

        public async Task<IEnumerable<Client>> GetUnlinkedClientsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            // On récupère les clients qui n'ont pas de UserAccount associé
            return await context.Clients
                .Where(c => c.UserAccount == null && c.Actif)
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        private string GenerateTemporaryPassword(int length = 10)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            var sb = new StringBuilder();
            var rand = new Random();
            while (0 < length--) { sb.Append(validChars[rand.Next(validChars.Length)]); }
            return sb.ToString();
        }
		
		public async Task<bool> UnlockAccountAsync(Guid id)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var user = await context.Utilisateurs.FindAsync(id);
			if (user == null) return false;

			user.DateVerrouillage = null;
			user.TentativesConnexionEchouees = 0;
			await context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ChangePasswordManualAsync(Guid id, string newPassword)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var user = await context.Utilisateurs.FindAsync(id);
			if (user == null) return false;

			user.MotDePasseHash = BCryptNet.HashPassword(newPassword);
			// Optionnel : On peut décider si le changement manuel force une reconnexion ou non
			// user.DoitChangerMotDePasse = false; 
			await context.SaveChangesAsync();
			return true;
		}
		
		public async Task<int> DeleteUnconfirmedAccountsAsync(int hoursOld)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var threshold = DateTime.UtcNow.AddHours(-hoursOld);

            // On cherche les utilisateurs non confirmés ET dont le token est expiré ou vieux
            var usersToDelete = await context.Utilisateurs
                .Where(u => !u.EmailConfirme && u.DateCreation < threshold)
                .Include(u => u.Client) // Pour supprimer le client lié aussi
                .ToListAsync();

            if (!usersToDelete.Any()) return 0;

            foreach (var user in usersToDelete)
            {
                // Si un client a été créé spécifiquement pour cet user (cas de l'inscription web), on le supprime aussi
                if (user.ClientId.HasValue && user.Client != null)
                {
                    context.Clients.Remove(user.Client);
                }
                context.Utilisateurs.Remove(user);
            }

            return await context.SaveChangesAsync();
        }
		

        public async Task<bool> ToggleEmailConfirmationAsync(Guid userId, bool isConfirmed)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FindAsync(userId);
            if (user == null) return false;

            user.EmailConfirme = isConfirmed;
            
            // Si on invalide l'email, on peut aussi vouloir réinitialiser le token pour forcer une nouvelle validation propre
            if (!isConfirmed)
            {
                user.TokenVerificationEmail = Guid.NewGuid().ToString();
                user.DateExpirationTokenEmail = DateTime.UtcNow.AddDays(1);
            }

            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResendConfirmationEmailAdminAsync(Guid userId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Utilisateurs.FindAsync(userId);
            
            // On ne peut renvoyer que si l'utilisateur existe et que l'email n'est PAS confirmé
            if (user == null || user.EmailConfirme) return false;

            // On appelle la méthode de l'AuthService qui gère déjà la génération de token et l'envoi SMTP
            await _authenticationService.ResendConfirmationEmailAsync(user.Email);
            
            return true;
        }
		
    }
}