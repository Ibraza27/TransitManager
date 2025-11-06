using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using TransitManager.Core.Exceptions;

namespace TransitManager.Infrastructure.Services
{
    public class ClientService : IClientService
    {
        public event Action<Guid>? ClientStatisticsUpdated;

        private readonly TransitContext _context;
        private readonly INotificationService _notificationService;
        private readonly INotificationHubService _notificationHubService;

        public ClientService(TransitContext context, INotificationService notificationService, INotificationHubService notificationHubService)
        {
            _context = context;
            _notificationService = notificationService;
            _notificationHubService = notificationHubService;
        }

        public async Task<Client?> GetByIdAsync(Guid id)
        {
            return await _context.Clients
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(c => c.Colis)
                    .ThenInclude(colis => colis.Paiements)
                .Include(c => c.Vehicules)
                    .ThenInclude(vehicule => vehicule.Paiements)
                .Include(c => c.Paiements)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Client?> GetByCodeAsync(string code)
        {
            return await _context.Clients
                .Include(c => c.Colis)
                .Include(c => c.Paiements)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CodeClient == code);
        }

        public async Task<IEnumerable<Client>> GetAllAsync()
        {
            return await _context.Clients
                .IgnoreQueryFilters()
                .AsNoTracking()
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetActiveClientsAsync()
        {
            return await _context.Clients
                .Where(c => c.Actif)
                .AsNoTracking()
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetActiveClientsAsync();
            searchTerm = searchTerm.ToLower();
            return await _context.Clients
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
            if (await ExistsAsync(client.Email ?? "", client.TelephonePrincipal))
            {
                throw new InvalidOperationException("Un client avec cet email ou ce téléphone existe déjà.");
            }
            if (string.IsNullOrEmpty(client.CodeClient))
            {
                client.CodeClient = await GenerateUniqueCodeAsync(_context);
            }
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            await _notificationHubService.NotifyClientUpdated(client.Id);
            await _notificationService.NotifyAsync(
                "Nouveau client",
                $"Le client {client.NomComplet} a été créé avec succès."
            );
            return client;
        }

        public async Task<Client> UpdateAsync(Client clientFromUI)
        {
            if (await ExistsAsync(clientFromUI.Email ?? "", clientFromUI.TelephonePrincipal, clientFromUI.Id))
            {
                throw new InvalidOperationException("Un autre client avec cet email ou ce téléphone existe déjà.");
            }

            var clientInDb = await _context.Clients
                                          .IgnoreQueryFilters()
                                          .Include(c => c.Colis)
                                          .Include(c => c.Vehicules)
                                          .FirstOrDefaultAsync(c => c.Id == clientFromUI.Id);
            if (clientInDb == null)
            {
                throw new InvalidOperationException("Le client que vous essayez de modifier n'a pas été trouvé.");
            }
            _context.Entry(clientInDb).CurrentValues.SetValues(clientFromUI);

            _context.Entry(clientInDb).Property("RowVersion").OriginalValue = clientFromUI.RowVersion;
            await UpdateClientStatisticsAsync(clientInDb, _context);
            try
            {
                await _context.SaveChangesAsync();
                await _notificationHubService.NotifyClientUpdated(clientFromUI.Id);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync();
                if (databaseValues == null)
                {
                    throw new ConcurrencyException("Le client a été supprimé par un autre utilisateur. Impossible de sauvegarder.");
                }
                else
                {
                    throw new ConcurrencyException("Ce client a été modifié par un autre utilisateur. Vos modifications n'ont pas pu être enregistrées.");
                }
            }

            return clientInDb;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var client = await _context.Clients.Include(c => c.Colis).FirstOrDefaultAsync(c => c.Id == id);
            if (client == null) return false;
            if (client.Colis.Any(c => c.Statut != Core.Enums.StatutColis.Livre))
            {
                throw new InvalidOperationException("Impossible de supprimer un client ayant des colis non livrés.");
            }
            client.Actif = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetTotalCountAsync()
        {
            return await _context.Clients.CountAsync(c => c.Actif);
        }

        public async Task<int> GetNewClientsCountAsync(DateTime since)
        {
            return await _context.Clients
                .CountAsync(c => c.Actif && c.DateCreation >= since);
        }

        public async Task<IEnumerable<Client>> GetRecentClientsAsync(int count)
        {
            return await _context.Clients
                .Where(c => c.Actif)
                .AsNoTracking()
                .OrderByDescending(c => c.DateCreation)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetClientsWithUnpaidBalanceAsync()
        {
            return await _context.Clients
                .Where(c => c.Actif && c.Impayes > 0)
                .AsNoTracking()
                .OrderByDescending(c => c.Impayes)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalUnpaidBalanceAsync()
        {
            return await _context.Clients
                .Where(c => c.Actif)
                .SumAsync(c => c.Impayes);
        }

        public async Task<IEnumerable<Client>> GetClientsByConteneurAsync(Guid conteneurId)
        {
            return await _context.Clients
                .Where(c => c.Actif && c.Colis.Any(co => co.ConteneurId == conteneurId))
                .AsNoTracking()
                .Distinct()
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(string email, string telephone, Guid? excludeId = null)
        {
            var query = _context.Clients.AsNoTracking().Where(c => c.Actif);
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
            await context.Entry(client).Collection(c => c.Colis).LoadAsync();
            await context.Entry(client).Collection(c => c.Vehicules).LoadAsync();
            decimal impayesColis = client.Colis.Where(c => c.Actif).Sum(c => c.RestantAPayer);
            decimal impayesVehicules = client.Vehicules.Where(v => v.Actif).Sum(v => v.RestantAPayer);
            client.Impayes = impayesColis + impayesVehicules;
            var conteneursColis = client.Colis.Where(c => c.ConteneurId.HasValue).Select(c => c.ConteneurId);
            var conteneursVehicules = client.Vehicules.Where(v => v.ConteneurId.HasValue).Select(v => v.ConteneurId);
            client.NombreConteneursUniques = conteneursColis.Union(conteneursVehicules).Distinct().Count();
        }

        public async Task RecalculateAndUpdateClientStatisticsAsync(Guid clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);

            if (client != null)
            {
                await UpdateClientStatisticsAsync(client, _context);
                await _context.SaveChangesAsync();
                ClientStatisticsUpdated?.Invoke(clientId);
            }
        }

        public async Task<Dictionary<string, int>> GetNewClientsPerMonthAsync(int months)
        {
            var result = new Dictionary<string, int>();

            for (int i = months - 1; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                var firstDayOfMonth = new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);
                var count = await _context.Clients
                    .CountAsync(c => c.DateInscription >= firstDayOfMonth && c.DateInscription < firstDayOfNextMonth);

                result.Add(firstDayOfMonth.ToString("MMM yy"), count);
            }
            return result;
        }
    }
}
