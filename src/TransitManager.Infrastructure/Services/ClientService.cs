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
				.Include(c => c.Vehicules) // <-- LIGNE AJOUTÉE
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

			// ÉTAPE A : Charger l'entité originale depuis la BDD. C'est elle qui est suivie par EF Core.
			var clientInDb = await context.Clients
										  .IgnoreQueryFilters() // Important pour pouvoir modifier un client inactif
										  .Include(c => c.Colis)
										  .Include(c => c.Vehicules) // On inclut les véhicules
										  .FirstOrDefaultAsync(c => c.Id == clientFromUI.Id);

			if (clientInDb == null)
			{
				throw new InvalidOperationException("Le client que vous essayez de modifier n'a pas été trouvé.");
			}

			// ÉTAPE B : Copier les valeurs modifiées depuis l'interface utilisateur vers l'entité suivie.
			context.Entry(clientInDb).CurrentValues.SetValues(clientFromUI);

			// ÉTAPE C : Appeler notre nouvelle méthode pour recalculer toutes les statistiques.
			await UpdateClientStatisticsAsync(clientInDb, context);

			// ÉTAPE D : Sauvegarder toutes les modifications en une seule transaction.
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
                .Where(c => c.Actif && c.Impayes > 0) 
                .AsNoTracking()
                .OrderByDescending(c => c.Impayes)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalUnpaidBalanceAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Clients
                .Where(c => c.Actif)
                .SumAsync(c => c.Impayes);
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
			// Charger explicitement les colis et les véhicules si ce n'est pas déjà fait
			await context.Entry(client).Collection(c => c.Colis).LoadAsync();
			await context.Entry(client).Collection(c => c.Vehicules).LoadAsync();

			// Calcul du total dû
			decimal totalDuColis = client.Colis.Where(c => c.Actif).Sum(c => c.PrixTotal);
			decimal totalDuVehicules = client.Vehicules.Where(v => v.Actif).Sum(v => v.PrixTotal);
			decimal totalDu = totalDuColis + totalDuVehicules;

			// Calcul du total payé
			decimal totalPayeColis = client.Colis.Where(c => c.Actif).Sum(c => c.SommePayee);
			decimal totalPayeVehicules = client.Vehicules.Where(v => v.Actif).Sum(v => v.SommePayee);
			decimal totalPaye = totalPayeColis + totalPayeVehicules;

			// Mise à jour de la propriété Impayes
			client.Impayes = totalDu - totalPaye;

			// Calcul du nombre de conteneurs uniques
			var conteneursColis = client.Colis.Where(c => c.ConteneurId.HasValue).Select(c => c.ConteneurId);
			var conteneursVehicules = client.Vehicules.Where(v => v.ConteneurId.HasValue).Select(v => v.ConteneurId);
			client.NombreConteneursUniques = conteneursColis.Union(conteneursVehicules).Distinct().Count();
		}
		
    }
}