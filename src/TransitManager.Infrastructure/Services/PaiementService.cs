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

        public PaiementService(
            IDbContextFactory<TransitContext> contextFactory,
            INotificationService notificationService,
            IExportService exportService,
            IClientService clientService,
            IMessenger messenger)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
            _exportService = exportService;
			_clientService = clientService;
            _messenger = messenger;
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

			// On s'assure que les dates sont en UTC pour la comparaison
			var debutUtc = debut.ToUniversalTime();
			var finUtc = fin.ToUniversalTime();

			return await context.Paiements
				.Include(p => p.Client)
				.Include(p => p.Conteneur)
				.Where(p => p.DatePaiement >= debutUtc && p.DatePaiement <= finUtc) // La condition est correcte ici
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

            context.Paiements.Add(paiement);
            await UpdateClientBalanceAsync(paiement.ClientId, context);
            await context.SaveChangesAsync();
			 _messenger.Send(new PaiementUpdatedMessage());
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
			 _messenger.Send(new PaiementUpdatedMessage());
			// Ligne à ajouter/vérifier
			await _clientService.RecalculateAndUpdateClientStatisticsAsync(paiement.ClientId);
			return paiement;
		}

        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var paiement = await context.Paiements.FindAsync(id);
            if (paiement == null) return false;

            if (paiement.Statut == StatutPaiement.Paye)
            {
                throw new InvalidOperationException("Impossible de supprimer un paiement validé.");
            }
			
			var clientId = paiement.ClientId;

            // Suppression logique
            paiement.Actif = false;
            await UpdateClientBalanceAsync(paiement.ClientId, context);
            await context.SaveChangesAsync();
			 _messenger.Send(new PaiementUpdatedMessage());
			await _clientService.RecalculateAndUpdateClientStatisticsAsync(clientId); 
            return true;
        }

		public async Task<bool> ValidatePaymentAsync(Guid paiementId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var paiement = await context.Paiements.FindAsync(paiementId);
			if (paiement == null) return false;

			paiement.Statut = StatutPaiement.Paye;
			await context.SaveChangesAsync();
			 _messenger.Send(new PaiementUpdatedMessage());
			// Ligne à ajouter/vérifier
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
			 _messenger.Send(new PaiementUpdatedMessage());
			// Ligne à ajouter/vérifier
			await _clientService.RecalculateAndUpdateClientStatisticsAsync(paiement.ClientId);
			return true;
        }

		public async Task<decimal> GetMonthlyRevenueAsync(DateTime month)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();

			// On s'assure que la date de début est bien le premier jour du mois à minuit, en UTC
			var debutMois = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
			var debutMoisSuivant = debutMois.AddMonths(1);

			return await context.Paiements
				.Where(p => p.Statut == StatutPaiement.Paye && 
							p.DatePaiement >= debutMois && 
							p.DatePaiement < debutMoisSuivant) // Utiliser "<" est plus sûr
				.SumAsync(p => p.Montant);
		}

        public async Task<decimal> GetPendingAmountAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var totalFacture = await context.Colis
                .Where(c => c.Actif)
                .SumAsync(c => c.ValeurDeclaree * 0.1m); 

            var totalPaye = await context.Paiements
                .Where(p => p.Statut == StatutPaiement.Paye)
                .SumAsync(p => p.Montant);

            return totalFacture - totalPaye;
        }

        public async Task<IEnumerable<Paiement>> GetOverduePaymentsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Paiements
                .Include(p => p.Client)
                .Where(p => p.DateEcheance.HasValue && p.DateEcheance.Value < DateTime.UtcNow && p.Statut != StatutPaiement.Paye)
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
			
			// CORRECTION APPLIQUÉE ICI
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
			// La méthode CreateAsync s'occupe déjà d'appeler la mise à jour.
			// Mais par sécurité, on peut s'assurer qu'elle est bien appelée après la création.
			await CreateAsync(paiement); 
			// Pas besoin de rajouter l'appel ici, car CreateAsync le fait déjà.
			return true;
        }
		
		public async Task<IEnumerable<Paiement>> GetByColisAsync(Guid colisId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			return await context.Paiements
				.Include(p => p.Client) // On garde le client au cas où
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
				
		private async Task UpdateClientBalanceAsync(Guid clientId, TransitContext context)
		{
			var client = await context.Clients.FindAsync(clientId);
			if (client != null)
			{
				client.Impayes = await CalculateClientBalanceAsync(clientId, context); // MODIFIÉ
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
				// totalDu = client.Colis.Where(c => c.Actif).Sum(c => c.PoidsFacturable * 2.5m); // ANCIENNE LIGNE
				totalDu = client.Colis.Where(c => c.Actif).Sum(c => c.PrixTotal); // NOUVELLE LIGNE
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