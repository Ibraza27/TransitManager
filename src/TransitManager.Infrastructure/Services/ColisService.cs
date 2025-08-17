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
        private readonly IConteneurService _conteneurService;

        public ColisService(IDbContextFactory<TransitContext> contextFactory, INotificationService notificationService, IConteneurService conteneurService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
            _conteneurService = conteneurService;
        }

        public async Task<Colis> CreateAsync(Colis colis)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            context.Colis.Add(colis);
            await context.SaveChangesAsync();
            
            // Si le colis a été directement affecté à un conteneur à la création
            if (colis.ConteneurId.HasValue)
            {
                await _conteneurService.RecalculateStatusAsync(colis.ConteneurId.Value);
            }

            return colis;
        }

        public async Task<Colis> UpdateAsync(Colis colis)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colisInDb = await context.Colis.Include(c => c.Barcodes).FirstOrDefaultAsync(c => c.Id == colis.Id);
            if (colisInDb == null) throw new InvalidOperationException("Le colis n'existe plus.");

            var originalConteneurId = colisInDb.ConteneurId;

            if (colis.Statut == StatutColis.Retourne)
            {
                colis.ConteneurId = null;
            }

            context.Entry(colisInDb).CurrentValues.SetValues(colis);
            colisInDb.ClientId = colis.ClientId;
            colisInDb.ConteneurId = colis.ConteneurId;
            
            var submittedBarcodeValues = new HashSet<string>(colis.Barcodes.Select(b => b.Value));
            var dbBarcodes = colisInDb.Barcodes.ToList();
            var barcodesToRemove = dbBarcodes.Where(b => b.Actif && !submittedBarcodeValues.Contains(b.Value)).ToList();
            foreach (var barcode in barcodesToRemove) barcode.Actif = false;
            
            var dbBarcodeValues = new HashSet<string>(dbBarcodes.Select(b => b.Value));
            var barcodesToAdd = submittedBarcodeValues.Where(value => !dbBarcodeValues.Contains(value))
                .Select(value => new Barcode { Value = value, ColisId = colisInDb.Id }).ToList();
                
            if (barcodesToAdd.Any()) await context.Barcodes.AddRangeAsync(barcodesToAdd);
            
            await context.SaveChangesAsync();

            if (originalConteneurId.HasValue)
            {
                await _conteneurService.RecalculateStatusAsync(originalConteneurId.Value);
            }
            if (colisInDb.ConteneurId.HasValue && colisInDb.ConteneurId != originalConteneurId)
            {
                await _conteneurService.RecalculateStatusAsync(colisInDb.ConteneurId.Value);
            }
            
            return colisInDb;
        }

        public async Task<bool> AssignToConteneurAsync(Guid colisId, Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.FindAsync(colisId);
            var conteneur = await context.Conteneurs.FindAsync(conteneurId);
            // On ajoute le statut "Probleme" à la liste des statuts valides pour une affectation
            var canReceiveStatuses = new[] { StatutConteneur.Reçu, StatutConteneur.EnPreparation, StatutConteneur.Probleme };
            if (colis == null || conteneur == null || !canReceiveStatuses.Contains(conteneur.Statut)) return false;
            colis.ConteneurId = conteneurId;
            colis.Statut = StatutColis.Affecte;
            colis.NumeroPlomb = conteneur.NumeroPlomb;
            await context.SaveChangesAsync();
            await _conteneurService.RecalculateStatusAsync(conteneurId);
            return true;
        }

        public async Task<bool> RemoveFromConteneurAsync(Guid colisId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.FindAsync(colisId);
            if (colis == null || !colis.ConteneurId.HasValue) return false;
            var originalConteneurId = colis.ConteneurId.Value;
            colis.ConteneurId = null;
            colis.Statut = StatutColis.EnAttente;
            colis.NumeroPlomb = null;
            await context.SaveChangesAsync();
            await _conteneurService.RecalculateStatusAsync(originalConteneurId);
            return true;
        }

        public async Task<Colis?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.Include(c => c.Client).Include(c => c.Conteneur).Include(c => c.Barcodes.Where(b => b.Actif)).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Colis?> GetByBarcodeAsync(string barcode)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.Include(c => c.Client).Include(c => c.Conteneur).Include(c => c.Barcodes.Where(b => b.Actif)).AsNoTracking().FirstOrDefaultAsync(c => c.Barcodes.Any(b => b.Value == barcode));
        }

        public async Task<Colis?> GetByReferenceAsync(string reference)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.Include(c => c.Client).Include(c => c.Conteneur).Include(c => c.Barcodes.Where(b => b.Actif)).AsNoTracking().FirstOrDefaultAsync(c => c.NumeroReference == reference);
        }

        public async Task<IEnumerable<Colis>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.Include(c => c.Client).Include(c => c.Conteneur).Include(c => c.Barcodes.Where(b => b.Actif)).AsNoTracking().OrderByDescending(c => c.DateArrivee).ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByClientAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.Include(c => c.Conteneur).Include(c => c.Barcodes.Where(b => b.Actif)).Where(c => c.ClientId == clientId).AsNoTracking().OrderByDescending(c => c.DateArrivee).ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByConteneurAsync(Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.Include(c => c.Client).Include(c => c.Barcodes.Where(b => b.Actif)).Where(c => c.ConteneurId == conteneurId).AsNoTracking().OrderBy(c => c.Client!.Nom).ThenBy(c => c.DateArrivee).ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetByStatusAsync(StatutColis statut)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.Include(c => c.Client).Include(c => c.Conteneur).Include(c => c.Barcodes.Where(b => b.Actif)).Where(c => c.Statut == statut).AsNoTracking().OrderByDescending(c => c.DateArrivee).ToListAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.Include(c => c.Barcodes).FirstOrDefaultAsync(c => c.Id == id);
            if (colis == null) return false;
            colis.Actif = false;
            foreach (var barcode in colis.Barcodes) barcode.Actif = false;
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

        public async Task<int> GetCountByStatusAsync(StatutColis statut)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.CountAsync(c => c.Statut == statut && c.Actif);
        }

        public async Task<IEnumerable<Colis>> GetRecentColisAsync(int count)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis.Include(c => c.Client).Include(c => c.Barcodes.Where(b => b.Actif)).Where(c => c.Actif).AsNoTracking().OrderByDescending(c => c.DateArrivee).Take(count).ToListAsync();
        }

        public async Task<IEnumerable<Colis>> GetColisWaitingLongTimeAsync(int days)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var dateLimit = DateTime.UtcNow.AddDays(-days);
            return await context.Colis.Include(c => c.Client).Include(c => c.Barcodes.Where(b => b.Actif)).Where(c => c.Actif && c.Statut == StatutColis.EnAttente && c.DateArrivee < dateLimit).AsNoTracking().OrderBy(c => c.DateArrivee).ToListAsync();
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
            return await context.Colis.Include(c => c.Client).Include(c => c.Conteneur).Include(c => c.Barcodes.Where(b => b.Actif)).Where(c => c.Actif && (c.Barcodes.Any(b => b.Value.ToLower().Contains(searchTerm)) || c.NumeroReference.ToLower().Contains(searchTerm) || c.Designation.ToLower().Contains(searchTerm) || (c.Client != null && (c.Client.Nom + " " + c.Client.Prenom).ToLower().Contains(searchTerm)) || (c.Conteneur != null && c.Conteneur.NumeroDossier.ToLower().Contains(searchTerm)))).AsNoTracking().OrderByDescending(c => c.DateArrivee).ToListAsync();
        }
    }
}