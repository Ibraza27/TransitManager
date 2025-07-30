using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Enums;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{

    public class ClientService : IClientService
    {
        private readonly TransitContext _context;
        private readonly INotificationService _notificationService;

        public ClientService(TransitContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<Client?> GetByIdAsync(Guid id)
        {
            return await _context.Clients
                .Include(c => c.Colis)
                .Include(c => c.Paiements)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Client?> GetByCodeAsync(string code)
        {
            return await _context.Clients
                .Include(c => c.Colis)
                .Include(c => c.Paiements)
                .FirstOrDefaultAsync(c => c.CodeClient == code);
        }

        public async Task<IEnumerable<Client>> GetAllAsync()
        {
            return await _context.Clients
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetActiveClientsAsync()
        {
            return await _context.Clients
                .Where(c => c.Actif)
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
                    c.Ville.ToLower().Contains(searchTerm)
                ))
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        public async Task<Client> CreateAsync(Client client)
        {
            // Validation
            if (await ExistsAsync(client.Email ?? "", client.TelephonePrincipal))
            {
                throw new InvalidOperationException("Un client avec cet email ou ce téléphone existe déjà.");
            }

            // Générer le code client s'il n'est pas défini
            if (string.IsNullOrEmpty(client.CodeClient))
            {
                client.CodeClient = await GenerateUniqueCodeAsync();
            }

            // Ajouter le client
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();

            // Notification
            await _notificationService.NotifyAsync(
                "Nouveau client",
                $"Le client {client.NomComplet} a été créé avec succès.",
                Core.Enums.TypeNotification.Succes
            );

            return client;
        }

        public async Task<Client> UpdateAsync(Client client)
        {
            // Validation
            if (await ExistsAsync(client.Email ?? "", client.TelephonePrincipal, client.Id))
            {
                throw new InvalidOperationException("Un autre client avec cet email ou ce téléphone existe déjà.");
            }

            // Mettre à jour les statistiques
            await UpdateClientStatisticsAsync(client);

            // Mettre à jour
            _context.Clients.Update(client);
            await _context.SaveChangesAsync();

            return client;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var client = await GetByIdAsync(id);
            if (client == null) return false;

            // Vérifier s'il a des colis actifs
            if (client.Colis.Any(c => c.Statut != Core.Enums.StatutColis.Livre))
            {
                throw new InvalidOperationException("Impossible de supprimer un client ayant des colis non livrés.");
            }

            // Suppression logique
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
                .OrderByDescending(c => c.DateCreation)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetClientsWithUnpaidBalanceAsync()
        {
            return await _context.Clients
                .Where(c => c.Actif && c.BalanceTotal > 0)
                .OrderByDescending(c => c.BalanceTotal)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalUnpaidBalanceAsync()
        {
            return await _context.Clients
                .Where(c => c.Actif)
                .SumAsync(c => c.BalanceTotal);
        }

        public async Task<bool> ExistsAsync(string email, string telephone, Guid? excludeId = null)
        {
            var query = _context.Clients.Where(c => c.Actif);

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

        public async Task<IEnumerable<Client>> GetClientsByConteneurAsync(Guid conteneurId)
        {
            return await _context.Clients
                .Where(c => c.Actif && c.Colis.Any(co => co.ConteneurId == conteneurId))
                .Distinct()
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            string code;
            do
            {
                var date = DateTime.Now.ToString("yyyyMMdd");
                var random = new Random().Next(1000, 9999);
                code = $"CLI-{date}-{random}";
            }
            while (await _context.Clients.AnyAsync(c => c.CodeClient == code));

            return code;
        }

        private async Task UpdateClientStatisticsAsync(Client client)
        {
            // Calculer le nombre total d'envois
            client.NombreTotalEnvois = await _context.Colis
                .CountAsync(c => c.ClientId == client.Id);

            // Calculer le volume total expédié
            client.VolumeTotalExpedié = await _context.Colis
                .Where(c => c.ClientId == client.Id)
                .SumAsync(c => c.Volume);

            // Calculer la balance totale (montants dus)
            var totalFacture = await _context.Colis
                .Where(c => c.ClientId == client.Id)
                .SumAsync(c => c.ValeurDeclaree * 0.1m); // Exemple : 10% de frais

            var totalPaye = await _context.Paiements
                .Where(p => p.ClientId == client.Id && p.Statut == Core.Enums.StatutPaiement.Paye)
                .SumAsync(p => p.Montant);

            client.BalanceTotal = totalFacture - totalPaye;

            // Déterminer si c'est un client fidèle
            if (client.NombreTotalEnvois >= 10 || client.VolumeTotalExpedié >= 100)
            {
                client.EstClientFidele = true;
                client.PourcentageRemise = 5; // 5% de remise pour les clients fidèles
            }
        }
    }
}