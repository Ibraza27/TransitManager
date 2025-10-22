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
using TransitManager.Core.Exceptions;

namespace TransitManager.Infrastructure.Services
{
    public class ColisService : IColisService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly INotificationService _notificationService;
        private readonly IConteneurService _conteneurService;
        private readonly IClientService _clientService;

        public ColisService(IDbContextFactory<TransitContext> contextFactory, INotificationService notificationService, IConteneurService conteneurService, IClientService clientService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
            _conteneurService = conteneurService;
            _clientService = clientService;
        }


		public async Task<Colis> CreateAsync(Colis colis)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // 1. On sépare les codes-barres du colis parent pour éviter les problèmes de suivi d'EF Core.
            var barcodesFromUI = colis.Barcodes.ToList();
            colis.Barcodes.Clear(); // On vide la collection sur l'objet principal.

            // 2. On ajoute UNIQUEMENT le colis au contexte. EF Core commence à le "suivre".
            context.Colis.Add(colis);

            // 3. MAINTENANT que le colis est suivi, on peut y ajouter ses enfants.
            foreach (var barcode in barcodesFromUI)
            {
                // On crée une NOUVELLE instance de Barcode pour être sûr qu'elle n'est pas déjà suivie ailleurs.
                var newBarcode = new Barcode
                {
                    Value = barcode.Value,
                    Colis = colis // On établit la relation de navigation. EF Core déduira le ColisId.
                };
                colis.Barcodes.Add(newBarcode);
            }

            // 4. On sauvegarde tout en une seule transaction.
            await context.SaveChangesAsync();

            // 5. Le reste de la logique ne change pas.
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(colis.ClientId);

            if (colis.ConteneurId.HasValue)
            {
                await _conteneurService.RecalculateStatusAsync(colis.ConteneurId.Value);
            }

            return colis;
        }

		public async Task<Colis> UpdateAsync(Colis colisFromUI)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            
            // On charge l'entité de la BDD et on inclut ses relations
            var colisInDb = await context.Colis
                .Include(c => c.Barcodes)
                .FirstOrDefaultAsync(c => c.Id == colisFromUI.Id);

            if (colisInDb == null) 
            {
                throw new InvalidOperationException("Le colis n'existe plus.");
            }

            var originalConteneurId = colisInDb.ConteneurId;
            var originalClientId = colisInDb.ClientId;

            // 1. Appliquer les changements simples sur l'entité suivie
            context.Entry(colisInDb).CurrentValues.SetValues(colisFromUI);
            colisInDb.ClientId = colisFromUI.ClientId; // S'assurer que les clés étrangères sont à jour
            colisInDb.ConteneurId = colisFromUI.ConteneurId;
            context.Entry(colisInDb).Property("RowVersion").OriginalValue = colisFromUI.RowVersion;

            // 2. Synchroniser la collection de codes-barres
            var barcodesFromUIValues = colisFromUI.Barcodes.Select(b => b.Value).ToHashSet();
            var barcodesInDb = colisInDb.Barcodes.ToList();

            // Supprimer les codes-barres qui ne sont plus dans la liste de l'UI
            var barcodesToRemove = barcodesInDb.Where(b => !barcodesFromUIValues.Contains(b.Value)).ToList();
            if (barcodesToRemove.Any())
            {
                context.Barcodes.RemoveRange(barcodesToRemove);
            }

            // Ajouter les nouveaux codes-barres
            var barcodesInDbValues = barcodesInDb.Select(b => b.Value).ToHashSet();
            var barcodeValuesToAdd = barcodesFromUIValues.Where(uiValue => !barcodesInDbValues.Contains(uiValue));
            foreach (var valueToAdd in barcodeValuesToAdd)
            {
                colisInDb.Barcodes.Add(new Barcode { Value = valueToAdd });
            }

            try
            {
                // 3. Sauvegarder TOUTES les modifications en une seule transaction
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var entry = ex.Entries.Single();
                var databaseValues = await entry.GetDatabaseValuesAsync();
                if (databaseValues == null)
                {
                    throw new ConcurrencyException("Ce colis a été supprimé par un autre utilisateur.");
                }
                else
                {
                    throw new ConcurrencyException("Ce colis a été modifié par un autre utilisateur. Vos modifications n'ont pas pu être enregistrées.");
                }
            }

            // 4. Appeler les services de mise à jour des statistiques APRÈS la sauvegarde.
            // Chaque service utilisera son propre contexte, ce qui est maintenant sûr.
			await _clientService.RecalculateAndUpdateClientStatisticsAsync(colisInDb.ClientId);
            // Si le client a changé, on met aussi à jour l'ancien client
            if (originalClientId != colisInDb.ClientId)
            {
                await _clientService.RecalculateAndUpdateClientStatisticsAsync(originalClientId);
            }

            // Mettre à jour l'ancien et le nouveau conteneur si nécessaire
            if (originalConteneurId.HasValue && originalConteneurId.Value != colisInDb.ConteneurId)
            {
                await _conteneurService.RecalculateStatusAsync(originalConteneurId.Value);
            }
            if(colisInDb.ConteneurId.HasValue)
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
            // On charge le colis en ignorant les filtres pour lui-même
            var colis = await context.Colis
                .IgnoreQueryFilters()
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes.Where(b => b.Actif))
                .Include(c => c.Paiements)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
            if (colis != null)
            {
                // Ensuite, on charge son client SÉPARÉMENT, en ignorant les filtres pour le client.
                // C'est la garantie absolue de l'obtenir.
                colis.Client = await context.Clients
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == colis.ClientId);
            }

            return colis;
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
            var clientId = colis.ClientId;
            colis.Actif = false;
            foreach (var barcode in colis.Barcodes) barcode.Actif = false;
            await context.SaveChangesAsync();
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(clientId);
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
            var searchTerms = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var query = context.Colis
                               .Include(c => c.Client)
                               .Include(c => c.Conteneur)
                               .Include(c => c.Barcodes.Where(b => b.Actif))
                               .AsNoTracking();
            foreach (var term in searchTerms)
            {
                query = query.Where(c =>
                    EF.Functions.ILike(c.NumeroReference, $"%{term}%") ||
                    EF.Functions.ILike(c.Designation, $"%{term}%") ||
                    (c.Client != null && EF.Functions.ILike(c.Client.NomComplet, $"%{term}%")) ||
                    (c.Conteneur != null && EF.Functions.ILike(c.Conteneur.NumeroDossier, $"%{term}%")) ||
                    (c.Destinataire != null && EF.Functions.ILike(c.Destinataire, $"%{term}%")) ||
                    c.Barcodes.Any(b => EF.Functions.ILike(b.Value, $"%{term}%"))
                );
            }

            return await query.OrderByDescending(c => c.DateArrivee).ToListAsync();
        }

        public async Task<Dictionary<StatutColis, int>> GetStatisticsByStatusAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Colis
                .Where(c => c.Actif)
                .GroupBy(c => c.Statut)
                .ToDictionaryAsync(g => g.Key, g => g.Count());
        }
    }
}
