using Microsoft.EntityFrameworkCore;
using System;
using TransitManager.Core.Entities; 
using TransitManager.Infrastructure.Services; // Ce using est nouveau, pour IExportService
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{

    public class PaiementService : IPaiementService
    {
        private readonly TransitContext _context;
        private readonly INotificationService _notificationService;
        private readonly IExportService _exportService;

        public PaiementService(
            TransitContext context, 
            INotificationService notificationService,
            IExportService exportService)
        {
            _context = context;
            _notificationService = notificationService;
            _exportService = exportService;
        }

        public async Task<Paiement?> GetByIdAsync(Guid id)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Conteneur)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Paiement>> GetAllAsync()
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Conteneur)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByClientAsync(Guid clientId)
        {
            return await _context.Paiements
                .Include(p => p.Conteneur)
                .Where(p => p.ClientId == clientId)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByConteneurAsync(Guid conteneurId)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Where(p => p.ConteneurId == conteneurId)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByPeriodAsync(DateTime debut, DateTime fin)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Conteneur)
                .Where(p => p.DatePaiement >= debut && p.DatePaiement <= fin)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<Paiement> CreateAsync(Paiement paiement)
        {
            // Validation
            var client = await _context.Clients.FindAsync(paiement.ClientId);
            if (client == null)
            {
                throw new InvalidOperationException("Client non trouvé.");
            }

            // Générer le numéro de reçu
            if (string.IsNullOrEmpty(paiement.NumeroRecu))
            {
                paiement.NumeroRecu = await GenerateUniqueReceiptNumberAsync();
            }

            // Ajouter le paiement
            _context.Paiements.Add(paiement);
            
            // Mettre à jour la balance du client
            await UpdateClientBalanceAsync(paiement.ClientId);

            await _context.SaveChangesAsync();

            // Notification
            await _notificationService.NotifyAsync(
                "Paiement reçu",
                $"Paiement de {paiement.Montant:C} reçu de {client.NomComplet}",
                TypeNotification.PaiementRecu
            );

            return paiement;
        }

        public async Task<Paiement> UpdateAsync(Paiement paiement)
        {
            _context.Paiements.Update(paiement);
            
            // Mettre à jour la balance du client
            await UpdateClientBalanceAsync(paiement.ClientId);
            
            await _context.SaveChangesAsync();

            return paiement;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var paiement = await GetByIdAsync(id);
            if (paiement == null) return false;

            // Vérifier si le paiement peut être supprimé
            if (paiement.Statut == StatutPaiement.Paye)
            {
                throw new InvalidOperationException("Impossible de supprimer un paiement validé.");
            }

            _context.Paiements.Remove(paiement);
            
            // Mettre à jour la balance du client
            await UpdateClientBalanceAsync(paiement.ClientId);
            
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ValidatePaymentAsync(Guid paiementId)
        {
            var paiement = await GetByIdAsync(paiementId);
            if (paiement == null) return false;

            paiement.Statut = StatutPaiement.Paye;
            await UpdateAsync(paiement);

            // Générer automatiquement le reçu
            await GenerateReceiptAsync(paiementId);

            return true;
        }

        public async Task<bool> CancelPaymentAsync(Guid paiementId, string raison)
        {
            var paiement = await GetByIdAsync(paiementId);
            if (paiement == null) return false;

            paiement.Statut = StatutPaiement.Annule;
            paiement.Commentaires = $"Annulé: {raison}";
            
            await UpdateAsync(paiement);

            return true;
        }

        public async Task<decimal> GetMonthlyRevenueAsync(DateTime month)
        {
            var debut = new DateTime(month.Year, month.Month, 1);
            var fin = debut.AddMonths(1).AddDays(-1);

            return await _context.Paiements
                .Where(p => p.DatePaiement >= debut && 
                           p.DatePaiement <= fin &&
                           p.Statut == StatutPaiement.Paye)
                .SumAsync(p => p.Montant);
        }

        public async Task<decimal> GetPendingAmountAsync()
        {
            // Calculer le total des montants en attente
            var totalFacture = await _context.Colis
                .Where(c => c.Actif)
                .SumAsync(c => c.ValeurDeclaree * 0.1m); // Exemple: 10% de frais

            var totalPaye = await _context.Paiements
                .Where(p => p.Statut == StatutPaiement.Paye)
                .SumAsync(p => p.Montant);

            return totalFacture - totalPaye;
        }

        public async Task<IEnumerable<Paiement>> GetOverduePaymentsAsync()
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Where(p => p.EstEnRetard && p.Statut != StatutPaiement.Paye)
                .OrderBy(p => p.DateEcheance)
                .ToListAsync();
        }

        public async Task<Dictionary<TypePaiement, decimal>> GetPaymentsByTypeAsync(DateTime debut, DateTime fin)
        {
            var paiements = await GetByPeriodAsync(debut, fin);
            
            return paiements
                .Where(p => p.Statut == StatutPaiement.Paye)
                .GroupBy(p => p.ModePaiement)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Montant));
        }

        public async Task<bool> SendPaymentReminderAsync(Guid clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return false;

            var balance = await GetClientBalanceAsync(clientId);
            if (balance <= 0) return false;

            // Envoyer la notification
            await _notificationService.NotifyAsync(
                "Rappel de paiement",
                $"Rappel: Le client {client.NomComplet} a un solde impayé de {balance:C}",
                TypeNotification.RetardPaiement,
                PrioriteNotification.Haute
            );

            // Mettre à jour les paiements en retard
            var paiementsEnRetard = await _context.Paiements
                .Where(p => p.ClientId == clientId && p.EstEnRetard)
                .ToListAsync();

            foreach (var paiement in paiementsEnRetard)
            {
                paiement.RappelEnvoye = true;
                paiement.DateDernierRappel = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // TODO: Envoyer un email/SMS au client

            return true;
        }

        public async Task<byte[]> GenerateReceiptAsync(Guid paiementId)
        {
            var paiement = await GetByIdAsync(paiementId);
            if (paiement == null)
            {
                throw new InvalidOperationException("Paiement non trouvé.");
            }

            // Utiliser le service d'export pour générer le reçu
            return await _exportService.GenerateReceiptPdfAsync(paiement);
        }

        public async Task<decimal> GetClientBalanceAsync(Guid clientId)
        {
            var client = await _context.Clients
                .Include(c => c.Colis)
                .Include(c => c.Paiements)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null) return 0;

            // Calculer le total dû (basé sur les colis)
            var totalDu = 0m;
            foreach (var colis in client.Colis.Where(c => c.Actif))
            {
                // Tarif basé sur le poids facturable
                var tarif = colis.PoidsFacturable * 2.5m; // Exemple: 2.5€/kg
                
                // Ajouter les frais de dédouanement si applicable
                if (colis.Conteneur?.TypeEnvoi == TypeEnvoi.AvecDedouanement)
                {
                    tarif += colis.ValeurDeclaree * 0.1m; // 10% de la valeur déclarée
                }
                
                totalDu += tarif;
            }

            // Appliquer la remise client fidèle
            if (client.EstClientFidele && client.PourcentageRemise > 0)
            {
                totalDu *= (1 - client.PourcentageRemise / 100);
            }

            // Soustraire les paiements effectués
            var totalPaye = client.Paiements
                .Where(p => p.Statut == StatutPaiement.Paye)
                .Sum(p => p.Montant);

            return totalDu - totalPaye;
        }

        public async Task<bool> RecordPartialPaymentAsync(Guid clientId, decimal montant, TypePaiement type, string? reference = null)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return false;

            var paiement = new Paiement
            {
                ClientId = clientId,
                Montant = montant,
                ModePaiement = type,
                Reference = reference,
                Description = "Paiement partiel",
                Statut = StatutPaiement.Valide,
                DatePaiement = DateTime.UtcNow
            };

            await CreateAsync(paiement);
            return true;
        }

        private async Task<string> GenerateUniqueReceiptNumberAsync()
        {
            string numero;
            do
            {
                var year = DateTime.Now.ToString("yyyy");
                var month = DateTime.Now.ToString("MM");
                var day = DateTime.Now.ToString("dd");
                var random = new Random().Next(1000, 9999);
                numero = $"REC-{year}{month}{day}-{random}";
            }
            while (await _context.Paiements.AnyAsync(p => p.NumeroRecu == numero));

            return numero;
        }

        private async Task UpdateClientBalanceAsync(Guid clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client != null)
            {
                client.BalanceTotal = await GetClientBalanceAsync(clientId);
                await _context.SaveChangesAsync();
            }
        }
    }
}