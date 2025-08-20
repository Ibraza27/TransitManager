using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class ClientService : IClientService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly INotificationService _notificationService;

        public ClientService(IDbContextFactory<TransitContext> contextFactory, INotificationService notificationService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
        }
		
        public async Task<Client?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients
				.IgnoreQueryFilters()
                .Include(c => c.Colis)
                .Include(c => c.Paiements)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Client?> GetByCodeAsync(string code)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients
                .Include(c => c.Colis)
                .Include(c => c.Paiements)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CodeClient == code);
        }


        public async Task<IEnumerable<Client>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            // On utilise IgnoreQueryFilters() pour récupérer VRAIMENT tous les clients.
            return await context.Clients
                .IgnoreQueryFilters() 
                .AsNoTracking()
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetActiveClientsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients
                .Where(c => c.Actif)
                .AsNoTracking()
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> SearchAsync(string searchTerm)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveClientsAsync();

            searchTerm = searchTerm.ToLower();
            return await context.Clients
                .Where(c => c.Actif && (
                    c.CodeClient.ToLower().Contains(searchTerm) ||
                    c.Nom.ToLower().Contains(searchTerm) ||
                    c.Prenom.ToLower().Contains(searchTerm) ||
                    c.TelephonePrincipal.Contains(searchTerm) ||
                    (c.TelephoneSecondaire != null && c.TelephoneSecondaire.Contains(searchTerm)) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                    (c.Ville != null && c.Ville.ToLower().Contains(searchTerm))
                ))
                .AsNoTracking()
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<Client> CreateAsync(Client client)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (await ExistsAsync(client.Email ?? "", client.TelephonePrincipal))
            {
                throw new InvalidOperationException("Un client avec cet email ou ce téléphone existe déjà.");
            }

            if (string.IsNullOrEmpty(client.CodeClient))
            {
                client.CodeClient = await GenerateUniqueCodeAsync(context);
            }

            context.Clients.Add(client);
            await context.SaveChangesAsync();

            await _notificationService.NotifyAsync(
                "Nouveau client",
                $"Le client {client.NomComplet} a été créé avec succès."
            );

            return client;
        }

        public async Task<Client> UpdateAsync(Client clientFromUI)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            if (await ExistsAsync(clientFromUI.Email ?? "", clientFromUI.TelephonePrincipal, clientFromUI.Id))
            {
                throw new InvalidOperationException("Un autre client avec cet email ou ce téléphone existe déjà.");
            }

            // ÉTAPE 1: Charger l'entité originale depuis la BDD (c'est elle qui est "suivie").
            var clientInDb = await context.Clients
                                          .IgnoreQueryFilters() // Important pour pouvoir modifier un client inactif
                                          .Include(c => c.Colis)
                                          .FirstOrDefaultAsync(c => c.Id == clientFromUI.Id);

            if (clientInDb == null)
            {
                throw new InvalidOperationException("Le client que vous essayez de modifier n'a pas été trouvé.");
            }

            // ÉTAPE 2: Copier les propriétés modifiées depuis l'objet de l'UI vers l'objet de la BDD.
            context.Entry(clientInDb).CurrentValues.SetValues(clientFromUI);

            // ÉTAPE 3: Recalculer les statistiques sur l'objet suivi par EF.
            await UpdateClientStatisticsAsync(clientInDb, context);

            // ÉTAPE 4: Sauvegarder les changements. EF sait ce qui a changé sur clientInDb.
            await context.SaveChangesAsync();

            return clientInDb;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var client = await context.Clients.Include(c => c.Colis).FirstOrDefaultAsync(c => c.Id == id);
            if (client == null) return false;

            if (client.Colis.Any(c => c.Statut != Core.Enums.StatutColis.Livre))
            {
                throw new InvalidOperationException("Impossible de supprimer un client ayant des colis non livrés.");
            }

            client.Actif = false;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalCountAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients.CountAsync(c => c.Actif);
        }

        public async Task<int> GetNewClientsCountAsync(DateTime since)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients
                .CountAsync(c => c.Actif && c.DateCreation >= since);
        }

        public async Task<IEnumerable<Client>> GetRecentClientsAsync(int count)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients
                .Where(c => c.Actif)
                .AsNoTracking()
                .OrderByDescending(c => c.DateCreation)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetClientsWithUnpaidBalanceAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients
                .Where(c => c.Actif && c.BalanceTotal > 0)
                .AsNoTracking()
                .OrderByDescending(c => c.BalanceTotal)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalUnpaidBalanceAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients
                .Where(c => c.Actif)
                .SumAsync(c => c.BalanceTotal);
        }

        public async Task<IEnumerable<Client>> GetClientsByConteneurAsync(Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients
                .Where(c => c.Actif && c.Colis.Any(co => co.ConteneurId == conteneurId))
                .AsNoTracking()
                .Distinct()
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string email, string telephone, Guid? excludeId = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Clients.AsNoTracking().Where(c => c.Actif);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            if (!string.IsNullOrEmpty(email))
            {
                if (await query.AnyAsync(c => c.Email == email))
                    return true;
            }

            return await query.AnyAsync(c => c.TelephonePrincipal == telephone);
        }

        private async Task<string> GenerateUniqueCodeAsync(TransitContext context)
        {
            string code;
            do
            {
                var date = DateTime.Now.ToString("yyyyMMdd");
                var random = new Random().Next(1000, 9999);
                code = $"CLI-{date}-{random}";
            }
            while (await context.Clients.AnyAsync(c => c.CodeClient == code));
            return code;
        }

        private async Task UpdateClientStatisticsAsync(Client client, TransitContext context)
        {
            // La logique est maintenant plus simple car les colis sont déjà chargés avec le client.
            client.NombreTotalEnvois = client.Colis.Count(c => c.Actif);
            client.VolumeTotalExpedié = client.Colis.Where(c => c.Actif).Sum(c => c.Volume);

            var totalPaye = await context.Paiements
                .Where(p => p.ClientId == client.Id && p.Statut == Core.Enums.StatutPaiement.Paye)
                .SumAsync(p => p.Montant);

            var totalFacture = client.Colis.Where(c => c.Actif).Sum(c => c.PrixTotal);
            client.BalanceTotal = totalFacture - totalPaye;

            if (client.NombreTotalEnvois >= 10 || client.VolumeTotalExpedié >= 100)
            {
                client.EstClientFidele = true;
                client.PourcentageRemise = 5;
            }
            else
            {
                client.EstClientFidele = false;
                client.PourcentageRemise = 0;
            }
        }
    }
}