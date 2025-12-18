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
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly INotificationService _notificationService;
        private readonly IExportService _exportService;
        private readonly IClientService _clientService;
        private readonly IMessenger _messenger;
        private readonly IVehiculeService _vehiculeService;
        private readonly IColisService _colisService;
		private readonly ITimelineService _timelineService;

        public PaiementService(
            IDbContextFactory<TransitContext> contextFactory,
            INotificationService notificationService,
            IExportService exportService,
            IClientService clientService,
            IMessenger messenger,
            IVehiculeService vehiculeService,
            IColisService colisService,
			ITimelineService timelineService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
            _exportService = exportService;
            _clientService = clientService;
            _messenger = messenger;
            _vehiculeService = vehiculeService;
            _colisService = colisService;
			_timelineService = timelineService;
        }

        public async Task<Paiement?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Conteneur)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Paiement>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Paiements
                .Include(p => p.Client)
                .Include(p => p.Conteneur)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByClientAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Paiements
                .Include(p => p.Conteneur)
                .Where(p => p.ClientId == clientId)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByConteneurAsync(Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Paiements
                .Include(p => p.Client)
                .Where(p => p.ConteneurId == conteneurId)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByPeriodAsync(DateTime debut, DateTime fin)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var debutUtc = debut.ToUniversalTime();
            var finUtc = fin.ToUniversalTime();
            var finInclusive = finUtc.Date.AddDays(1).AddTicks(-1);
                return await context.Paiements
                    .Include(p => p.Client)
                    .Include(p => p.Conteneur)
                    .Where(p => p.DatePaiement >= debutUtc && p.DatePaiement <= finInclusive)
                    .AsNoTracking()
                    .OrderByDescending(p => p.DatePaiement)
                    .ToListAsync();
        }

		public async Task<Paiement> CreateAsync(Paiement paiement)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var client = await context.Clients.FindAsync(paiement.ClientId);
			if (client == null)
			{
				throw new InvalidOperationException("Client non trouvé.");
			}
			if (string.IsNullOrEmpty(paiement.NumeroRecu))
			{
				paiement.NumeroRecu = await GenerateUniqueReceiptNumberAsync(context);
			}
			paiement.Statut = StatutPaiement.Paye;
			context.Paiements.Add(paiement);
			await context.SaveChangesAsync();

			await _timelineService.AddEventAsync(
				$"Paiement reçu : {paiement.Montant:C} ({paiement.ModePaiement})",
				colisId: paiement.ColisId,
				vehiculeId: paiement.VehiculeId,
				conteneurId: paiement.ConteneurId
			);

			var clientUser = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == paiement.ClientId);
			
			// Notif Client
			if (clientUser != null) {
				await _notificationService.CreateAndSendAsync(
					"💰 Paiement Reçu",
					$"Paiement de {paiement.Montant:C} validé.",
					clientUser.Id,
					CategorieNotification.Paiement,
					actionUrl: GetPaiementActionUrl(paiement),
					relatedEntityId: paiement.Id,
					relatedEntityType: "Paiement"
				);
			}

			// Notif Admin
			await _notificationService.CreateAndSendAsync(
				"💰 Nouveau Paiement",
				$"Paiement de {paiement.Montant:C} ({client.NomComplet})",
				null, // Admin
				CategorieNotification.Paiement,
				actionUrl: GetPaiementActionUrl(paiement), // <--- URL CORRIGÉE
				relatedEntityId: paiement.Id,
				relatedEntityType: "Paiement"
			);

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
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Paiements.Update(paiement);
            await context.SaveChangesAsync();
			
			await _notificationService.CreateAndSendAsync(
				"Modification Paiement",
				$"Le paiement {paiement.NumeroRecu} a été modifié.",
				null, // Admin seulement pour modif
				CategorieNotification.Paiement,
				actionUrl: GetPaiementActionUrl(paiement),
				relatedEntityId: paiement.Id,
				relatedEntityType: "Paiement"
			);
			
            await _timelineService.AddEventAsync(
                $"Paiement mis à jour : {paiement.Montant:C} ({paiement.ModePaiement})",
                colisId: paiement.ColisId,
                vehiculeId: paiement.VehiculeId,
                conteneurId: paiement.ConteneurId
            );
			
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
            await using var context = await _contextFactory.CreateDbContextAsync();
            var paiement = await context.Paiements.FindAsync(id);
            if (paiement == null) return false;
            var clientId = paiement.ClientId;
            var vehiculeId = paiement.VehiculeId;
            var colisId = paiement.ColisId;
            paiement.Actif = false;
            await context.SaveChangesAsync();
			
			// Après SaveChangesAsync (Attention, l'objet est supprimé/inactif, pas de redirection possible vers lui-même)
			// On redirige vers le parent
			string parentUrl = "";
			if (paiement.ColisId.HasValue) parentUrl = $"/colis/edit/{paiement.ColisId}";
			else if (paiement.VehiculeId.HasValue) parentUrl = $"/vehicule/edit/{paiement.VehiculeId}";

			await _notificationService.CreateAndSendAsync(
				"Suppression Paiement",
				$"Le paiement {paiement.NumeroRecu} de {paiement.Montant:C} a été supprimé.",
				null, // Admin
				CategorieNotification.Paiement,
				actionUrl: parentUrl,
				priorite: PrioriteNotification.Haute
			);
			
            await _timelineService.AddEventAsync(
                $"Paiement suprimer : {paiement.Montant:C} ({paiement.ModePaiement})",
                colisId: paiement.ColisId,
                vehiculeId: paiement.VehiculeId,
                conteneurId: paiement.ConteneurId
            );
			
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
            await using var context = await _contextFactory.CreateDbContextAsync();
            var paiement = await context.Paiements.FindAsync(paiementId);
            if (paiement == null) return false;
            paiement.Statut = StatutPaiement.Paye;
			
            await _timelineService.AddEventAsync(
                $"Paiement reçu : {paiement.Montant:C} ({paiement.ModePaiement})",
                colisId: paiement.ColisId,
                vehiculeId: paiement.VehiculeId,
                conteneurId: paiement.ConteneurId
            );
			
            await context.SaveChangesAsync();
            _messenger.Send(new PaiementUpdatedMessage());
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(paiement.ClientId);
            return true;
        }

        public async Task<bool> CancelPaymentAsync(Guid paiementId, string raison)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var paiement = await context.Paiements.FindAsync(paiementId);
            if (paiement == null) return false;
            paiement.Statut = StatutPaiement.Annule;
            paiement.Commentaires = $"Annulé : {raison}";
            await context.SaveChangesAsync();
			
            await _timelineService.AddEventAsync(
                $"Paiement suprimer : {paiement.Montant:C} ({paiement.ModePaiement})",
                colisId: paiement.ColisId,
                vehiculeId: paiement.VehiculeId,
                conteneurId: paiement.ConteneurId
            );
			
            _messenger.Send(new PaiementUpdatedMessage());
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(paiement.ClientId);
            return true;
        }

		public async Task<decimal> GetMonthlyRevenueAsync(DateTime month)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var debutMois = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
			var debutMoisSuivant = debutMois.AddMonths(1);
			return await context.Paiements
				.Where(p => p.Actif && p.Statut == StatutPaiement.Paye &&
							p.DatePaiement >= debutMois &&
							p.DatePaiement < debutMoisSuivant)
				.SumAsync(p => p.Montant);
		}
		
		public async Task<decimal> GetPendingAmountAsync()
		{
			// Ceci est une approximation. Une vraie logique se baserait sur des factures.
			// Pour le moment, on retourne le total des impayés des clients.
			await using var context = await _contextFactory.CreateDbContextAsync();
			return await context.Clients.Where(c => c.Actif).SumAsync(c => c.Impayes);
		}

		public async Task<IEnumerable<Paiement>> GetOverduePaymentsAsync()
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			return await context.Paiements
				.Include(p => p.Client)
				.Where(p => p.Actif && p.DateEcheance.HasValue && p.DateEcheance.Value < DateTime.UtcNow && p.Statut != StatutPaiement.Paye)
				.AsNoTracking()
				.OrderBy(p => p.DateEcheance)
				.ToListAsync();
		}

        public async Task<Dictionary<TypePaiement, decimal>> GetPaymentsByTypeAsync(DateTime debut, DateTime fin)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Paiements
                .Where(p => p.DatePaiement >= debut && p.DatePaiement <= fin && p.Statut == StatutPaiement.Paye)
                .GroupBy(p => p.ModePaiement)
                .ToDictionaryAsync(g => g.Key, g => g.Sum(p => p.Montant));
        }

        public async Task<bool> SendPaymentReminderAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var client = await context.Clients.FindAsync(clientId);
            if (client == null) return false;
            var balance = await GetClientBalanceAsync(clientId);
            if (balance <= 0) return false;
            await _notificationService.NotifyAsync(
                "Rappel de paiement",
                $"Rappel: Le client {client.NomComplet} a un solde impayé de {balance:C}",
                TypeNotification.RetardPaiement,
                PrioriteNotification.Haute
            );
            var paiementsEnRetard = await context.Paiements
                .Where(p => p.ClientId == clientId && p.DateEcheance.HasValue && p.DateEcheance.Value < DateTime.UtcNow)
                .ToListAsync();
            foreach (var paiement in paiementsEnRetard)
            {
                paiement.RappelEnvoye = true;
                paiement.DateDernierRappel = DateTime.UtcNow;
            }
            await context.SaveChangesAsync();
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
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await CalculateClientBalanceAsync(clientId, context);
        }

        public async Task<bool> RecordPartialPaymentAsync(Guid clientId, decimal montant, TypePaiement type, string? reference = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
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
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Paiements
                .Include(p => p.Client)
                .Where(p => p.ColisId == colisId)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
        }

        public async Task<IEnumerable<Paiement>> GetByVehiculeAsync(Guid vehiculeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Paiements
                .Where(p => p.VehiculeId == vehiculeId)
                .AsNoTracking()
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();
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
                totalDu += client.Colis.Where(c => c.Actif).Sum(c => c.PrixTotal);
            }
            
            // Include Vehicles
			var vehicles = await context.Vehicules
				.Where(v => v.ClientId == clientId && v.Actif)
				.AsNoTracking()
				.ToListAsync();
				
			if (vehicles.Any())
			{
				totalDu += vehicles.Sum(v => v.PrixTotal);
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
		
		private string GetPaiementActionUrl(Paiement p)
		{
			if (p.ColisId.HasValue) return $"/colis/edit/{p.ColisId}";
			if (p.VehiculeId.HasValue) return $"/vehicule/edit/{p.VehiculeId}";
			if (p.ConteneurId.HasValue) return $"/conteneur/detail/{p.ConteneurId}";
			return "/finance"; // Fallback
		}
		
		
        public async Task<Dictionary<string, decimal>> GetMonthlyRevenueHistoryAsync(int months)
        {
             await using var context = await _contextFactory.CreateDbContextAsync();
            var limitDate = DateTime.UtcNow.AddMonths(-months);
            
            var data = await context.Paiements
                .Where(p => p.DatePaiement >= limitDate && p.Statut == StatutPaiement.Paye)
                .GroupBy(p => new { p.DatePaiement.Year, p.DatePaiement.Month })
                .Select(g => new { 
                    Year = g.Key.Year, 
                    Month = g.Key.Month, 
                    Total = g.Sum(x => x.Montant) 
                })
                .ToListAsync();

            var result = new Dictionary<string, decimal>();
            for (int i = 0; i < months; i++)
            {
                var d = DateTime.UtcNow.AddMonths(-i);
                var key = d.ToString("MMM yyyy"); 
                var entry = data.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                result[key] = entry?.Total ?? 0;
            }
            return result;
        }

    }
}
