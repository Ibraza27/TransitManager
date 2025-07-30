using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services // <-- Notez le bon namespace
{
    public class ColisService : IColisService
	{
		private readonly TransitContext _context;
		private readonly INotificationService _notificationService;
		private readonly IBarcodeService _barcodeService;

		public ColisService(
			TransitContext context, 
			INotificationService notificationService,
			IBarcodeService barcodeService)
		{
			_context = context;
			_notificationService = notificationService;
			_barcodeService = barcodeService;
		}

		public async Task<Colis?> GetByIdAsync(Guid id)
		{
			return await _context.Colis
				.Include(c => c.Client)
				.Include(c => c.Conteneur)
				.FirstOrDefaultAsync(c => c.Id == id);
		}

		public async Task<Colis?> GetByBarcodeAsync(string barcode)
		{
			return await _context.Colis
				.Include(c => c.Client)
				.Include(c => c.Conteneur)
				.FirstOrDefaultAsync(c => c.CodeBarre == barcode);
		}

		public async Task<Colis?> GetByReferenceAsync(string reference)
		{
			return await _context.Colis
				.Include(c => c.Client)
				.Include(c => c.Conteneur)
				.FirstOrDefaultAsync(c => c.NumeroReference == reference);
		}

		public async Task<IEnumerable<Colis>> GetAllAsync()
		{
			return await _context.Colis
				.Include(c => c.Client)
				.Include(c => c.Conteneur)
				.OrderByDescending(c => c.DateArrivee)
				.ToListAsync();
		}

		public async Task<IEnumerable<Colis>> GetByClientAsync(Guid clientId)
		{
			return await _context.Colis
				.Include(c => c.Conteneur)
				.Where(c => c.ClientId == clientId)
				.OrderByDescending(c => c.DateArrivee)
				.ToListAsync();
		}

		public async Task<IEnumerable<Colis>> GetByConteneurAsync(Guid conteneurId)
		{
			return await _context.Colis
				.Include(c => c.Client)
				.Where(c => c.ConteneurId == conteneurId)
				.OrderBy(c => c.Client!.Nom)
				.ThenBy(c => c.DateArrivee)
				.ToListAsync();
		}

		public async Task<IEnumerable<Colis>> GetByStatusAsync(StatutColis statut)
		{
			return await _context.Colis
				.Include(c => c.Client)
				.Include(c => c.Conteneur)
				.Where(c => c.Statut == statut)
				.OrderByDescending(c => c.DateArrivee)
				.ToListAsync();
		}

		public async Task<Colis> CreateAsync(Colis colis)
		{
			// Valider le client
			var clientExists = await _context.Clients.AnyAsync(c => c.Id == colis.ClientId);
			if (!clientExists)
			{
				throw new InvalidOperationException("Le client spécifié n'existe pas.");
			}

			// Générer le code-barres s'il n'est pas défini
			if (string.IsNullOrEmpty(colis.CodeBarre))
			{
				colis.CodeBarre = await GenerateUniqueBarcodeAsync();
			}

			// Ajouter au contexte
			_context.Colis.Add(colis);
			await _context.SaveChangesAsync();

			// Notification
			var client = await _context.Clients.FindAsync(colis.ClientId);
			await _notificationService.NotifyAsync(
				"Nouveau colis",
				$"Un nouveau colis ({colis.NumeroReference}) a été enregistré pour {client?.NomComplet}.",
				TypeNotification.NouveauColis
			);

			// Générer l'étiquette avec code-barres
			await _barcodeService.GenerateLabelAsync(colis);

			return colis;
		}

		public async Task<Colis> UpdateAsync(Colis colis)
		{
			// Enregistrer les changements dans l'historique
			await LogColisHistoryAsync(colis);

			_context.Colis.Update(colis);
			await _context.SaveChangesAsync();

			return colis;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			var colis = await GetByIdAsync(id);
			if (colis == null) return false;

			// Vérifier si le colis peut être supprimé
			if (colis.Statut == StatutColis.EnTransit || colis.Statut == StatutColis.Livre)
			{
				throw new InvalidOperationException("Impossible de supprimer un colis en transit ou déjà livré.");
			}

			// Suppression logique
			colis.Actif = false;
			await _context.SaveChangesAsync();

			return true;
		}

		public async Task<Colis> ScanAsync(string barcode, string location)
		{
			var colis = await GetByBarcodeAsync(barcode);
			if (colis == null)
			{
				throw new InvalidOperationException($"Aucun colis trouvé avec le code-barres {barcode}");
			}

			// Enregistrer le scan
			var scanHistory = new
			{
				Date = DateTime.UtcNow,
				Location = location,
				Status = colis.Statut.ToString(),
				User = "Current User" // TODO: Récupérer depuis le contexte
			};

			// Ajouter à l'historique
			var history = string.IsNullOrEmpty(colis.HistoriqueScan) 
				? new List<object>() 
				: JsonSerializer.Deserialize<List<object>>(colis.HistoriqueScan) ?? new List<object>();
			
			history.Add(scanHistory);
			colis.HistoriqueScan = JsonSerializer.Serialize(history);
			colis.DateDernierScan = DateTime.UtcNow;
			colis.LocalisationActuelle = location;

			await UpdateAsync(colis);

			return colis;
		}

		public async Task<bool> AssignToConteneurAsync(Guid colisId, Guid conteneurId)
		{
			var colis = await GetByIdAsync(colisId);
			if (colis == null) return false;

			var conteneur = await _context.Conteneurs.FindAsync(conteneurId);
			if (conteneur == null) return false;

			// Vérifier que le conteneur peut recevoir des colis
			if (!conteneur.PeutRecevoirColis)
			{
				throw new InvalidOperationException("Le conteneur ne peut plus recevoir de colis.");
			}

			// Assigner le colis
			colis.ConteneurId = conteneurId;
			colis.Statut = StatutColis.Affecte;

			await UpdateAsync(colis);

			// Notification
			await _notificationService.NotifyAsync(
				"Colis affecté",
				$"Le colis {colis.NumeroReference} a été affecté au conteneur {conteneur.NumeroDossier}.",
				TypeNotification.Information
			);

			return true;
		}

		public async Task<bool> RemoveFromConteneurAsync(Guid colisId)
		{
			var colis = await GetByIdAsync(colisId);
			if (colis == null) return false;

			if (colis.Statut == StatutColis.EnTransit || colis.Statut == StatutColis.Livre)
			{
				throw new InvalidOperationException("Impossible de retirer un colis en transit ou livré.");
			}

			colis.ConteneurId = null;
			colis.Statut = StatutColis.EnAttente;

			await UpdateAsync(colis);
			return true;
		}

		public async Task<int> GetCountByStatusAsync(StatutColis statut)
		{
			return await _context.Colis.CountAsync(c => c.Statut == statut && c.Actif);
		}

		public async Task<IEnumerable<Colis>> GetRecentColisAsync(int count)
		{
			return await _context.Colis
				.Include(c => c.Client)
				.Where(c => c.Actif)
				.OrderByDescending(c => c.DateArrivee)
				.Take(count)
				.ToListAsync();
		}

		public async Task<IEnumerable<Colis>> GetColisWaitingLongTimeAsync(int days)
		{
			var dateLimit = DateTime.UtcNow.AddDays(-days);
			
			return await _context.Colis
				.Include(c => c.Client)
				.Where(c => c.Actif && 
					   c.Statut == StatutColis.EnAttente && 
					   c.DateArrivee < dateLimit)
				.OrderBy(c => c.DateArrivee)
				.ToListAsync();
		}

		public async Task<bool> MarkAsDeliveredAsync(Guid colisId, string signature)
		{
			var colis = await GetByIdAsync(colisId);
			if (colis == null) return false;

			colis.Statut = StatutColis.Livre;
			colis.DateLivraison = DateTime.UtcNow;
			colis.SignatureReception = signature;

			await UpdateAsync(colis);

			// Notification
			await _notificationService.NotifyAsync(
				"Colis livré",
				$"Le colis {colis.NumeroReference} a été livré avec succès.",
				TypeNotification.Succes
			);

			return true;
		}

		public async Task<IEnumerable<Colis>> SearchAsync(string searchTerm)
		{
			if (string.IsNullOrWhiteSpace(searchTerm))
				return await GetAllAsync();

			searchTerm = searchTerm.ToLower();

			return await _context.Colis
				.Include(c => c.Client)
				.Include(c => c.Conteneur)
				.Where(c => c.Actif && (
					c.CodeBarre.Contains(searchTerm) ||
					c.NumeroReference.ToLower().Contains(searchTerm) ||
					c.Designation.ToLower().Contains(searchTerm) ||
					c.Client!.NomComplet.ToLower().Contains(searchTerm) ||
					(c.Conteneur != null && c.Conteneur.NumeroDossier.ToLower().Contains(searchTerm))
				))
				.OrderByDescending(c => c.DateArrivee)
				.ToListAsync();
		}

		private async Task<string> GenerateUniqueBarcodeAsync()
		{
			string barcode;
			do
			{
				barcode = _barcodeService.GenerateBarcode();
			}
			while (await _context.Colis.AnyAsync(c => c.CodeBarre == barcode));

			return barcode;
		}

		private async Task LogColisHistoryAsync(Colis colis)
		{
			// TODO: Implémenter l'enregistrement de l'historique des modifications
			await Task.CompletedTask;
		}
	}
}