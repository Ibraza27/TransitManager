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
            var colisInDb = await context.Colis.Include(c => c.Barcodes).FirstOrDefaultAsync(c => c.Id == colis.Id);
            if (colisInDb == null) throw new InvalidOperationException("Colis non trouvé.");

            context.Entry(colisInDb).CurrentValues.SetValues(colis);

            var barcodesToDelete = colisInDb.Barcodes.Where(db => !colis.Barcodes.Any(ui => ui.Value == db.Value)).ToList();
            foreach (var b in barcodesToDelete) { context.Barcodes.Remove(b); }

            var barcodesToAdd = colis.Barcodes.Where(ui => !colisInDb.Barcodes.Any(db => db.Value == ui.Value)).ToList();
            foreach (var b in barcodesToAdd) { colisInDb.Barcodes.Add(new Barcode { Value = b.Value }); }

            await context.SaveChangesAsync();
            return colisInDb;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.FindAsync(id);
            if (colis == null) return false;
            colis.Actif = false;
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

        // CORRECTION ICI : La méthode est maintenant "public"
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