using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using CommunityToolkit.Mvvm.Messaging;
using TransitManager.Core.Messages;

namespace TransitManager.Infrastructure.Services
{
    public class PaiementService : IPaiementService
    {
        private readonly TransitContext _context;
        private readonly INotificationService _notificationService;
        private readonly IExportService _exportService;
        private readonly IClientService _clientService;
        private readonly IMessenger _messenger;
        private readonly IVehiculeService _vehiculeService;
        private readonly IColisService _colisService;

        public PaiementService(
            TransitContext context,
            INotificationService notificationService,
            IExportService exportService,
            IClientService clientService,
            IMessenger messenger,
            IVehiculeService vehiculeService,
            IColisService colisService)
        {
            _context = context;
            _notificationService = notificationService;
            _exportService = exportService;
            _clientService = clientService;
            _messenger = messenger;
            _vehiculeService = vehiculeService;
            _colisService = colisService;
        }

        public async Task<Paiement?> GetByIdAsync(Guid id)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Conteneur)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Paiement>> GetAllAsync()
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Conteneur)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByClientAsync(Guid clientId)
        {
            return await _context.Paiements
                .Include(p => p.Conteneur)
                .Where(p => p.ClientId == clientId)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByConteneurAsync(Guid conteneurId)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Where(p => p.ConteneurId == conteneurId)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByPeriodAsync(DateTime debut, DateTime fin)
        {
            var debutUtc = debut.ToUniversalTime();
            var finUtc = fin.ToUniversalTime();
            return await _context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Conteneur)
                .Where(p => p.DatePaiement >= debutUtc && p.DatePaiement < finUtc)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<Paiement> CreateAsync(Paiement paiement)
        {
            var client = await _context.Clients.FindAsync(paiement.ClientId);
            if (client == null)
            {
                throw new InvalidOperationException("Client non trouvé.");
            }
            if (string.IsNullOrEmpty(paiement.NumeroRecu))
            {
                paiement.NumeroRecu = await GenerateUniqueReceiptNumberAsync(_context);
            }
            paiement.Statut = StatutPaiement.Paye;
            _context.Paiements.Add(paiement);
            await _context.SaveChangesAsync();
            _messenger.Send(new PaiementUpdatedMessage());
            if (paiement.VehiculeId.HasValue)
            {
                await _vehiculeService.RecalculateAndUpdateVehiculeStatisticsAsync(paiement.VehiculeId.Value);
            }
            if (paiement.ColisId.HasValue)
            {
                await _colisService.RecalculateAndUpdateColisStatisticsAsync(paiement.ColisId.Value);
            }

            await _clientService.RecalculateAndUpdateClientStatisticsAsync(paiement.ClientId);
            await _notificationService.NotifyAsync(
                "Paiement reçu",
                $"Paiement de {paiement.Montant:C} reçu de {client.NomComplet}"
            );
            return paiement;
        }

        public async Task<Paiement> UpdateAsync(Paiement paiement)
        {
            _context.Paiements.Update(paiement);
            await _context.SaveChangesAsync();
            _messenger.Send(new PaiementUpdatedMessage());

            if (paiement.VehiculeId.HasValue)
            {
                await _vehiculeService.RecalculateAndUpdateVehiculeStatisticsAsync(paiement.VehiculeId.Value);
            }
            if (paiement.ColisId.HasValue)
            {
                await _colisService.RecalculateAndUpdateColisStatisticsAsync(paiement.ColisId.Value);
            }
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(paiement.ClientId);
            return paiement;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var paiement = await _context.Paiements.FindAsync(id);
            if (paiement == null) return false;

            var clientId = paiement.ClientId;
            var vehiculeId = paiement.VehiculeId;
            var colisId = paiement.ColisId;
            paiement.Actif = false;
            await _context.SaveChangesAsync();
            _messenger.Send(new PaiementUpdatedMessage());

            if (vehiculeId.HasValue)
            {
                await _vehiculeService.RecalculateAndUpdateVehiculeStatisticsAsync(vehiculeId.Value);
            }
            if (colisId.HasValue)
            {
                await _colisService.RecalculateAndUpdateColisStatisticsAsync(colisId.Value);
            }
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(clientId);
            return true;
        }

        public async Task<bool> ValidatePaymentAsync(Guid paiementId)
        {
            var paiement = await _context.Paiements.FindAsync(paiementId);
            if (paiement == null) return false;
            paiement.Statut = StatutPaiement.Paye;
            await _context.SaveChangesAsync();
            _messenger.Send(new PaiementUpdatedMessage());
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(paiement.ClientId);
            return true;
        }

        public async Task<bool> CancelPaymentAsync(Guid paiementId, string raison)
        {
            var paiement = await _context.Paiements.FindAsync(paiementId);
            if (paiement == null) return false;
            paiement.Statut = StatutPaiement.Annule;
            paiement.Commentaires = $"Annulé : {raison}";
            await _context.SaveChangesAsync();
            _messenger.Send(new PaiementUpdatedMessage());
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(paiement.ClientId);
            return true;
        }

        public async Task<decimal> GetMonthlyRevenueAsync(DateTime month)
        {
            var debutMois = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var debutMoisSuivant = debutMois.AddMonths(1);
            return await _context.Paiements
                .Where(p => p.Statut == StatutPaiement.Paye &&
                            p.DatePaiement >= debutMois &&
                            p.DatePaiement < debutMoisSuivant)
                .SumAsync(p => p.Montant);
        }

        public async Task<decimal> GetPendingAmountAsync()
        {
            var totalFacture = await _context.Colis
                .Where(c => c.Actif)
                .SumAsync(c => c.ValeurDeclaree * 0.1m);
            var totalPaye = await _context.Paiements
                .Where(p => p.Statut == StatutPaiement.Paye)
                .SumAsync(p => p.Montant);
            return totalFacture - totalPaye;
        }

        public async Task<IEnumerable<Paiement>> GetOverduePaymentsAsync()
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Where(p => p.DateEcheance.HasValue && p.DateEcheance.Value < DateTime.UtcNow && p.Statut != StatutPaiement.Paye)
                .AsNoTracking()
                .OrderBy(p => p.DateEcheance)
                .ToListAsync();
        }

        public async Task<Dictionary<TypePaiement, decimal>> GetPaymentsByTypeAsync(DateTime debut, DateTime fin)
        {
            return await _context.Paiements
                .Where(p => p.DatePaiement >= debut && p.DatePaiement <= fin && p.Statut == StatutPaiement.Paye)
                .GroupBy(p => p.ModePaiement)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(p => p.Montant));
        }

        public async Task<bool> SendPaymentReminderAsync(Guid clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client == null) return false;
            var balance = await GetClientBalanceAsync(clientId);
            if (balance <= 0) return false;
            await _notificationService.NotifyAsync(
                "Rappel de paiement",
                $"Rappel: Le client {client.NomComplet} a un solde impayé de {balance:C}",
                TypeNotification.RetardPaiement,
                PrioriteNotification.Haute
            );
            var paiementsEnRetard = await _context.Paiements
                .Where(p => p.ClientId == clientId && p.DateEcheance.HasValue && p.DateEcheance.Value < DateTime.UtcNow)
                .ToListAsync();
            foreach (var paiement in paiementsEnRetard)
            {
                paiement.RappelEnvoye = true;
                paiement.DateDernierRappel = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
            _messenger.Send(new PaiementUpdatedMessage());

            await _clientService.RecalculateAndUpdateClientStatisticsAsync(clientId);

            return true;
        }

        public async Task<byte[]> GenerateReceiptAsync(Guid paiementId)
        {
            var paiement = await GetByIdAsync(paiementId);
            if (paiement == null)
            {
                throw new InvalidOperationException("Paiement non trouvé.");
            }
            return await _exportService.GenerateReceiptPdfAsync(paiement);
        }

        public async Task<decimal> GetClientBalanceAsync(Guid clientId)
        {
            return await CalculateClientBalanceAsync(clientId, _context);
        }

        public async Task<bool> RecordPartialPaymentAsync(Guid clientId, decimal montant, TypePaiement type, string? reference = null)
        {
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

        public async Task<IEnumerable<Paiement>> GetByColisAsync(Guid colisId)
        {
            return await _context.Paiements
                .Include(p => p.Client)
                .Where(p => p.ColisId == colisId)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByVehiculeAsync(Guid vehiculeId)
        {
            return await _context.Paiements
                .Where(p => p.VehiculeId == vehiculeId)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        private async Task UpdateClientBalanceAsync(Guid clientId, TransitContext context)
        {
            var client = await context.Clients.FindAsync(clientId);
            if (client != null)
            {
                client.Impayes = await CalculateClientBalanceAsync(clientId, context);
            }
        }

        private async Task<decimal> CalculateClientBalanceAsync(Guid clientId, TransitContext context)
        {
            var client = await context.Clients
                .Include(c => c.Colis)
                .ThenInclude(co => co.Conteneur)
                .Include(c => c.Paiements)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clientId);
            if (client == null) return 0;
            decimal totalDu = 0;
            if (client.Colis != null)
            {
                totalDu = client.Colis.Where(c => c.Actif).Sum(c => c.PrixTotal);
            }
            var totalPaye = client.Paiements?.Where(p => p.Statut == StatutPaiement.Paye).Sum(p => p.Montant) ?? 0;
            return totalDu - totalPaye;
        }

        private async Task<string> GenerateUniqueReceiptNumberAsync(TransitContext context)
        {
            string numero;
            do
            {
                var date = DateTime.Now;
                numero = $"REC-{date:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            }
            while (await context.Paiements.AnyAsync(p => p.NumeroRecu == numero));
            return numero;
        }
    }
}
