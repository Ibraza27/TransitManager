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

namespace TransitManager.Infrastructure.Services
{
    public class ColisService : IColisService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly INotificationService _notificationService;
        private readonly IBarcodeService _barcodeService;

        public ColisService(
            IDbContextFactory<TransitContext> contextFactory,
            INotificationService notificationService,
            IBarcodeService barcodeService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
            _barcodeService = barcodeService;
        }

        public async Task<Colis?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Colis?> GetByBarcodeAsync(string barcode)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Barcodes.Any(b => b.Value == barcode));
        }

        public async Task<Colis?> GetByReferenceAsync(string reference)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.NumeroReference == reference);
        }

        public async Task<IEnumerable<Colis>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes)
                .AsNoTracking()
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByClientAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes)
                .Where(c => c.ClientId == clientId)
                .AsNoTracking()
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByConteneurAsync(Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis
                .Include(c => c.Client)
                .Include(c => c.Barcodes)
                .Where(c => c.ConteneurId == conteneurId)
                .AsNoTracking()
                .OrderBy(c => c.Client!.Nom)
                .ThenBy(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByStatusAsync(StatutColis statut)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes)
                .Where(c => c.Statut == statut)
                .AsNoTracking()
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }

        public async Task<Colis> CreateAsync(Colis colis)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Colis.Add(colis);
            await context.SaveChangesAsync();
            return colis;
        }

		public async Task<Colis> UpdateAsync(Colis colis)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			
			var colisInDb = await context.Colis
				.Include(c => c.Barcodes)
				.FirstOrDefaultAsync(c => c.Id == colis.Id);

			if (colisInDb == null)
			{
				throw new DbUpdateConcurrencyException("Le colis que vous essayez de modifier n'existe plus.");
			}

			context.Entry(colisInDb).CurrentValues.SetValues(colis);
			
			var submittedBarcodeValues = new HashSet<string>(colis.Barcodes.Select(b => b.Value.Trim()).Where(v => !string.IsNullOrEmpty(v)));
			var dbBarcodes = colisInDb.Barcodes.ToList();

			// 3a. Identifier et "supprimer doucement" les codes-barres qui existent en BDD mais plus dans l'interface
			var barcodesToRemove = dbBarcodes.Where(b => !submittedBarcodeValues.Contains(b.Value)).ToList();
			if (barcodesToRemove.Any())
			{
				// === LA CORRECTION EST ICI ===
				// Au lieu de supprimer physiquement, on les désactive.
				foreach (var barcode in barcodesToRemove)
				{
					barcode.Actif = false;
				}
			}

			// 3b. Identifier et ajouter les codes-barres qui sont dans l'interface mais pas encore en BDD
			var dbBarcodeValues = new HashSet<string>(dbBarcodes.Select(b => b.Value));
			var barcodesToAdd = submittedBarcodeValues
				.Where(value => !dbBarcodeValues.Contains(value))
				.Select(value => new Barcode { Value = value, ColisId = colisInDb.Id })
				.ToList();

			if (barcodesToAdd.Any())
			{
				await context.Barcodes.AddRangeAsync(barcodesToAdd);
			}
			
			context.Entry(colisInDb).Property(c => c.RowVersion).OriginalValue = colis.RowVersion;

			try
			{
				await context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException ex)
			{
				throw new InvalidOperationException("Les données du colis ont été modifiées par un autre utilisateur. Veuillez rafraîchir et réessayer.", ex);
			}
			
			return colisInDb;
		}

		public async Task<bool> DeleteAsync(Guid id)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			
			// On charge le colis ET ses codes-barres associés
			var colis = await context.Colis
				.Include(c => c.Barcodes)
				.FirstOrDefaultAsync(c => c.Id == id);

			if (colis == null) return false;

			// 1. Désactiver le colis lui-même
			colis.Actif = false;

			// 2. Désactiver tous les codes-barres liés
			foreach (var barcode in colis.Barcodes)
			{
				barcode.Actif = false;
			}

			// 3. Sauvegarder toutes les modifications en une seule fois
			await context.SaveChangesAsync();
			return true;
		}

        public async Task<Colis> ScanAsync(string barcode, string location)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.Include(c => c.Barcodes).FirstOrDefaultAsync(c => c.Barcodes.Any(b => b.Value == barcode));
            if (colis == null) throw new InvalidOperationException($"Colis introuvable avec le code-barres {barcode}");

            var history = string.IsNullOrEmpty(colis.HistoriqueScan) ? new List<object>() : JsonSerializer.Deserialize<List<object>>(colis.HistoriqueScan) ?? new List<object>();
            history.Add(new { Date = DateTime.UtcNow, Location = location, Status = colis.Statut.ToString() });
            colis.HistoriqueScan = JsonSerializer.Serialize(history);
            colis.DateDernierScan = DateTime.UtcNow;
            colis.LocalisationActuelle = location;
            await context.SaveChangesAsync();
            return colis;
        }

        public async Task<bool> AssignToConteneurAsync(Guid colisId, Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.FindAsync(colisId);
            var conteneur = await context.Conteneurs.FindAsync(conteneurId);
            if (colis == null || conteneur == null || !conteneur.PeutRecevoirColis) return false;

            colis.ConteneurId = conteneurId;
            colis.Statut = StatutColis.Affecte;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromConteneurAsync(Guid colisId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.FindAsync(colisId);
            if (colis == null) return false;
            colis.ConteneurId = null;
            colis.Statut = StatutColis.EnAttente;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCountByStatusAsync(StatutColis statut)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.CountAsync(c => c.Statut == statut && c.Actif);
        }

        public async Task<IEnumerable<Colis>> GetRecentColisAsync(int count)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.Include(c => c.Client).Include(c => c.Barcodes)
                .Where(c => c.Actif).AsNoTracking().OrderByDescending(c => c.DateArrivee).Take(count).ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetColisWaitingLongTimeAsync(int days)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var dateLimit = DateTime.UtcNow.AddDays(-days);
            return await context.Colis.Include(c => c.Client).Include(c => c.Barcodes)
                .Where(c => c.Actif && c.Statut == StatutColis.EnAttente && c.DateArrivee < dateLimit)
                .AsNoTracking().OrderBy(c => c.DateArrivee).ToListAsync();
        }

        public async Task<bool> MarkAsDeliveredAsync(Guid colisId, string signature)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.FindAsync(colisId);
            if (colis == null) return false;
            colis.Statut = StatutColis.Livre;
            colis.DateLivraison = DateTime.UtcNow;
            colis.SignatureReception = signature;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Colis>> SearchAsync(string searchTerm)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllAsync();
            
            searchTerm = searchTerm.ToLower();
            return await context.Colis
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes)
                .Where(c => c.Actif && (
                    c.Barcodes.Any(b => b.Value.ToLower().Contains(searchTerm)) ||
                    c.NumeroReference.ToLower().Contains(searchTerm) ||
                    c.Designation.ToLower().Contains(searchTerm) ||
                    (c.Client != null && (c.Client.Nom + " " + c.Client.Prenom).ToLower().Contains(searchTerm)) ||
                    (c.Conteneur != null && c.Conteneur.NumeroDossier.ToLower().Contains(searchTerm))
                ))
                .AsNoTracking()
                .OrderByDescending(c => c.DateArrivee)
                .ToListAsync();
        }
    }
}