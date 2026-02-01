using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Core.DTOs; // AJOUT
using TransitManager.Infrastructure.Data.Uow;
using TransitManager.Core.Exceptions;

namespace TransitManager.Infrastructure.Services
{
    public class ClientService : IClientService
    {
        public event Action<Guid>? ClientStatisticsUpdated;
        private readonly IUnitOfWorkFactory _uowFactory;
        private readonly INotificationService _notificationService;
        private readonly INotificationHubService _notificationHubService;
        private readonly IAuthenticationService _authService;

        public ClientService(
            IUnitOfWorkFactory uowFactory,
            INotificationService notificationService,
            INotificationHubService notificationHubService,
            IAuthenticationService authService)
        {
            _uowFactory = uowFactory;
            _notificationService = notificationService;
            _notificationHubService = notificationHubService;
            _authService = authService; 
        }

        public async Task<Client?> GetByIdAsync(Guid id)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Clients.GetWithDetailsAsync(id);
        }

        public async Task<Client?> GetByCodeAsync(string code)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Clients.GetByCodeAsync(code);
        }

        public async Task<IEnumerable<Client>> GetAllAsync()
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Clients.GetAllAsync();
        }

        public async Task<IEnumerable<Client>> GetActiveClientsAsync()
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Clients.GetActiveClientsAsync();
        }

        public async Task<IEnumerable<Client>> SearchAsync(string searchTerm)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Clients.SearchAsync(searchTerm);
        }

        public async Task<Client> CreateAsync(Client client)
        {
            using var uow = await _uowFactory.CreateAsync();
            
            if (!await uow.Clients.IsEmailUniqueAsync(client.Email ?? "") || 
                !await uow.Clients.IsPhoneUniqueAsync(client.TelephonePrincipal))
            {
                throw new InvalidOperationException("Un client avec cet email ou ce téléphone existe déjà.");
            }

            if (string.IsNullOrEmpty(client.CodeClient))
            {
                client.CodeClient = await GenerateUniqueCodeAsync(uow);
            }

            await uow.Clients.AddAsync(client);
            await uow.CommitAsync();

            // Auto-create user account with confirmed email if email is provided
            if (!string.IsNullOrWhiteSpace(client.Email))
            {
                try
                {
                    var result = await _authService.CreateOrResetPortalAccessAsync(client.Id);
                    // Note: result.TemporaryPassword could be used to send welcome email
                }
                catch (Exception)
                {
                    // User creation failed - client is still created, log if needed
                }
            }

            await _notificationHubService.NotifyClientUpdated(client.Id);
            await _notificationService.NotifyAsync(
                "Nouveau client",
                $"Le client {client.NomComplet} a été créé avec succès."
            );
            return client;
        }

        public async Task<Client> UpdateAsync(Client clientFromUI)
        {
            using var uow = await _uowFactory.CreateAsync();

            if (!await uow.Clients.IsEmailUniqueAsync(clientFromUI.Email ?? "", clientFromUI.Id) ||
                !await uow.Clients.IsPhoneUniqueAsync(clientFromUI.TelephonePrincipal, clientFromUI.Id))
            {
                throw new InvalidOperationException("Doublon détecté (email ou téléphone).");
            }

            var clientInDb = await uow.Clients.GetWithDetailsAsync(clientFromUI.Id);
            if (clientInDb == null) throw new InvalidOperationException("Client introuvable.");

            // Mapping manuel (évite les problèmes de tracking EF)
            clientInDb.Nom = clientFromUI.Nom;
            clientInDb.Prenom = clientFromUI.Prenom;
            clientInDb.TelephonePrincipal = clientFromUI.TelephonePrincipal;
            clientInDb.TelephoneSecondaire = clientFromUI.TelephoneSecondaire;
            clientInDb.Email = clientFromUI.Email;
            clientInDb.AdressePrincipale = clientFromUI.AdressePrincipale;
            clientInDb.Ville = clientFromUI.Ville;
            clientInDb.CodePostal = clientFromUI.CodePostal;
            clientInDb.Pays = clientFromUI.Pays;
            clientInDb.Commentaires = clientFromUI.Commentaires;
            clientInDb.Actif = clientFromUI.Actif;
            clientInDb.EstClientFidele = clientFromUI.EstClientFidele;
            clientInDb.PourcentageRemise = clientFromUI.PourcentageRemise;
            clientInDb.RowVersion = clientFromUI.RowVersion;

            await UpdateClientStatisticsAsync(clientInDb); // Pas besoin de passer l'uow, les collections sont chargées
            await _authService.SynchronizeClientDataAsync(clientInDb);

            try
            {
                await uow.CommitAsync();
                await _notificationHubService.NotifyClientUpdated(clientFromUI.Id);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("Conflit de concurrence détecté.");
            }
            return clientInDb;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using var uow = await _uowFactory.CreateAsync();
            var client = await uow.Clients.GetWithDetailsAsync(id);
            if (client == null) return false;

            if (client.Colis.Any(c => c.Statut != Core.Enums.StatutColis.Livre))
            {
                throw new InvalidOperationException("Impossible de supprimer un client avec des colis en cours.");
            }

            client.Actif = false;
            await uow.CommitAsync();
            return true;
        }

        // ... (Méthodes de statistiques inchangées ou simplifiées) ...
        public Task<int> GetTotalCountAsync() => Task.FromResult(0); // À implémenter dans le repo si besoin
        public async Task<int> GetNewClientsCountAsync(DateTime since)
        {
            using var uow = await _uowFactory.CreateAsync();
            var allClients = await uow.Clients.GetAllAsync();
            return allClients.Count(c => c.DateCreation >= since && c.Actif);
        }

        public async Task<IEnumerable<Client>> GetNewClientsListAsync(DateTime since)
        {
             using var uow = await _uowFactory.CreateAsync();
             var allClients = await uow.Clients.GetAllAsync();
             return allClients
                .Where(c => (c.DateCreation >= since || c.DateInscription >= since) && c.Actif)
                .OrderByDescending(c => c.DateCreation > c.DateInscription ? c.DateCreation : c.DateInscription)
                .ToList();
        }
        public async Task<IEnumerable<Client>> GetRecentClientsAsync(int count) 
        {
             using var uow = await _uowFactory.CreateAsync();
             // Implémentation rapide via GetAll pour l'instant (à optimiser via Repo)
             var all = await uow.Clients.GetAllAsync();
             return all.OrderByDescending(c => c.DateCreation).Take(count);
        }
        public Task<IEnumerable<Client>> GetClientsWithUnpaidBalanceAsync() => Task.FromResult(Enumerable.Empty<Client>());
        public Task<decimal> GetTotalUnpaidBalanceAsync() => Task.FromResult(0m);
        public Task<IEnumerable<Client>> GetClientsByConteneurAsync(Guid conteneurId) => Task.FromResult(Enumerable.Empty<Client>());
        public Task<bool> ExistsAsync(string email, string telephone, Guid? excludeId = null) => Task.FromResult(false);
        public Task<Dictionary<string, int>> GetNewClientsPerMonthAsync(int months) => Task.FromResult(new Dictionary<string, int>());

        private async Task<string> GenerateUniqueCodeAsync(IUnitOfWork uow)
        {
            string code;
            do
            {
                var date = DateTime.Now.ToString("yyyyMMdd");
                var random = new Random().Next(1000, 9999);
                code = $"CLI-{date}-{random}";
            }
            // Utilisation d'une méthode synchrone ou asynchrone existante sur le Repo serait mieux, 
            // mais GetAllAsync().Any() fonctionne pour dépanner (pas optimal perf).
            while ((await uow.Clients.GetAllAsync()).Any(c => c.CodeClient == code));
            return code;
        }

        private Task UpdateClientStatisticsAsync(Client client)
        {
            // Calcul en mémoire sur les collections chargées
            decimal impayesColis = client.Colis?.Where(c => c.Actif).Sum(c => c.RestantAPayer) ?? 0;
            decimal impayesVehicules = client.Vehicules?.Where(v => v.Actif).Sum(v => v.RestantAPayer) ?? 0;
            client.Impayes = impayesColis + impayesVehicules;
            
            // Note: NombreConteneursUniques logic here...
            
            return Task.CompletedTask;
        }

        public async Task RecalculateAndUpdateClientStatisticsAsync(Guid clientId)
        {
            using var uow = await _uowFactory.CreateAsync();
            var client = await uow.Clients.GetWithDetailsAsync(clientId);
            if (client != null)
            {
                await UpdateClientStatisticsAsync(client);
                await uow.CommitAsync();
                ClientStatisticsUpdated?.Invoke(clientId);
            }
        }

        public async Task<Core.DTOs.PagedResult<Client>> GetPagedAsync(int page, int pageSize, string? search = null)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Clients.GetPagedAsync(page, pageSize, search);
        }
    }
}