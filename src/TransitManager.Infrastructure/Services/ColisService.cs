// src/TransitManager.Infrastructure/Services/ColisService.cs

using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
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
        private readonly IClientService _clientService;

        public ColisService(
            IDbContextFactory<TransitContext> contextFactory,
            INotificationService notificationService,
            IConteneurService conteneurService,
            IClientService clientService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
            _conteneurService = conteneurService;
            _clientService = clientService;
        }


		public async Task RecalculateAndUpdateColisStatisticsAsync(Guid colisId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			var colis = await context.Colis.FindAsync(colisId);
			if (colis != null)
			{
				// Calculer la somme des paiements VALIDES ou PAYÉS
				var totalPaye = await context.Paiements
					.Where(p => p.ColisId == colisId && p.Actif && 
							   (p.Statut == StatutPaiement.Paye || p.Statut == StatutPaiement.Valide))
					.SumAsync(p => p.Montant);

				colis.SommePayee = totalPaye;
				
				// CRUCIAL : On doit sauvegarder ici
				await context.SaveChangesAsync(); 
			}
		}

        public async Task<Colis> CreateAsync(CreateColisDto colisDto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var newColis = new Colis
            {
                ClientId = colisDto.ClientId,
                Designation = colisDto.Designation,
                DestinationFinale = colisDto.DestinationFinale,
                NombrePieces = colisDto.NombrePieces,
                Volume = colisDto.Volume,
                ValeurDeclaree = colisDto.ValeurDeclaree,
                PrixTotal = colisDto.PrixTotal,
                Destinataire = colisDto.Destinataire,
                TelephoneDestinataire = colisDto.TelephoneDestinataire,
                LivraisonADomicile = colisDto.LivraisonADomicile,
                AdresseLivraison = colisDto.AdresseLivraison,
                EstFragile = colisDto.EstFragile,
                ManipulationSpeciale = colisDto.ManipulationSpeciale,
                InstructionsSpeciales = colisDto.InstructionsSpeciales,
                Type = colisDto.Type,
                TypeEnvoi = colisDto.TypeEnvoi,
                ConteneurId = colisDto.ConteneurId
            };
            foreach (var barcodeValue in colisDto.Barcodes)
            {
                newColis.Barcodes.Add(new Barcode { Value = barcodeValue });
            }
            context.Colis.Add(newColis);
            await context.SaveChangesAsync();
            
            // Les appels aux autres services sont ok car ils géreront leur propre contexte
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(newColis.ClientId);
            if (newColis.ConteneurId.HasValue)
            {
                await _conteneurService.RecalculateStatusAsync(newColis.ConteneurId.Value);
            }
            return newColis;
        }

        public async Task<Colis> UpdateAsync(Guid id, UpdateColisDto colisDto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            try
            {
                var colisInDb = await context.Colis.Include(c => c.Barcodes).FirstOrDefaultAsync(c => c.Id == id);
                if (colisInDb == null) throw new InvalidOperationException("Le colis n'existe plus.");
                
                var originalClientId = colisInDb.ClientId;
                var originalConteneurId = colisInDb.ConteneurId;
                
                // Mappage des propriétés
                colisInDb.ClientId = colisDto.ClientId;
                colisInDb.Designation = colisDto.Designation;
                colisInDb.DestinationFinale = colisDto.DestinationFinale;
				if (string.IsNullOrEmpty(colisInDb.InventaireJson) || colisInDb.InventaireJson == "[]")
				{
					 colisInDb.NombrePieces = colisDto.NombrePieces;
					 colisInDb.Volume = colisDto.Volume;
					 colisInDb.ValeurDeclaree = colisDto.ValeurDeclaree;
				}
                colisInDb.PrixTotal = colisDto.PrixTotal;
                colisInDb.Destinataire = colisDto.Destinataire;
                colisInDb.TelephoneDestinataire = colisDto.TelephoneDestinataire;
                colisInDb.LivraisonADomicile = colisDto.LivraisonADomicile;
                colisInDb.AdresseLivraison = colisDto.AdresseLivraison;
                colisInDb.EstFragile = colisDto.EstFragile;
                colisInDb.ManipulationSpeciale = colisDto.ManipulationSpeciale;
                colisInDb.InstructionsSpeciales = colisDto.InstructionsSpeciales;
                colisInDb.Type = colisDto.Type;
                colisInDb.TypeEnvoi = colisDto.TypeEnvoi;
                colisInDb.ConteneurId = colisDto.ConteneurId;
                colisInDb.Statut = colisDto.Statut;
                colisInDb.LieuSignatureInventaire = colisDto.LieuSignatureInventaire;
                colisInDb.DateSignatureInventaire = colisDto.DateSignatureInventaire;
                colisInDb.SignatureClientInventaire = colisDto.SignatureClientInventaire;

                context.Update(colisInDb);

                var barcodesFromUIValues = colisDto.Barcodes.ToHashSet();
                var barcodesInDb = colisInDb.Barcodes.ToList();
                var barcodesToRemove = barcodesInDb.Where(b => !barcodesFromUIValues.Contains(b.Value)).ToList();
                if (barcodesToRemove.Any())
                {
                    context.Barcodes.RemoveRange(barcodesToRemove);
                }
                var barcodesInDbValues = barcodesInDb.Select(b => b.Value).ToHashSet();
                var barcodeValuesToAdd = barcodesFromUIValues.Where(uiValue => !barcodesInDbValues.Contains(uiValue));
                foreach (var valueToAdd in barcodeValuesToAdd)
                {
                    context.Barcodes.Add(new Barcode { Value = valueToAdd, ColisId = colisInDb.Id });
                }

                await context.SaveChangesAsync();

                // Appels aux autres services après la sauvegarde
                await _clientService.RecalculateAndUpdateClientStatisticsAsync(colisInDb.ClientId);
                if (originalClientId != colisInDb.ClientId)
                {
                    await _clientService.RecalculateAndUpdateClientStatisticsAsync(originalClientId);
                }
                if (originalConteneurId.HasValue && originalConteneurId.Value != colisInDb.ConteneurId)
                {
                    await _conteneurService.RecalculateStatusAsync(originalConteneurId.Value);
                }
                if (colisInDb.ConteneurId.HasValue)
                {
                    await _conteneurService.RecalculateStatusAsync(colisInDb.ConteneurId.Value);
                }
                return colisInDb;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR ColisService.UpdateAsync] : {ex.Message}");
                Console.WriteLine(ex.InnerException?.Message);
                throw;
            }
        }


		public async Task UpdateInventaireAsync(UpdateInventaireDto dto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.FirstOrDefaultAsync(c => c.Id == dto.ColisId);
            
            if (colis != null)
            {
                // 1. Mise à jour des données inventaire
                colis.InventaireJson = dto.InventaireJson;
                colis.NombrePieces = dto.TotalPieces;
                colis.ValeurDeclaree = dto.TotalValeurDeclaree;

                // 2. Mise à jour de la signature (AJOUT)
                colis.LieuSignatureInventaire = dto.LieuSignatureInventaire;
                colis.DateSignatureInventaire = dto.DateSignatureInventaire;
                colis.SignatureClientInventaire = dto.SignatureClientInventaire;
                
                context.Colis.Update(colis);
                await context.SaveChangesAsync();
                
                await _clientService.RecalculateAndUpdateClientStatisticsAsync(colis.ClientId);
            }
        }

        public async Task<bool> AssignToConteneurAsync(Guid colisId, Guid conteneurId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var colis = await context.Colis.FindAsync(colisId);
            var conteneur = await context.Conteneurs.FindAsync(conteneurId);
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
            return await context.Colis
                .AsSplitQuery()
                .IgnoreQueryFilters()
                .Include(c => c.Client)
                .Include(c => c.Conteneur)
                .Include(c => c.Barcodes.Where(b => b.Actif))
                .Include(c => c.Paiements)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
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
		
		public async Task<IEnumerable<Colis>> GetByUserIdAsync(Guid userId)
		{
			await using var context = await _contextFactory.CreateDbContextAsync();
			
			// On cherche d'abord le ClientId lié à l'utilisateur
			var user = await context.Utilisateurs.FindAsync(userId);
			if (user == null || !user.ClientId.HasValue)
			{
				return Enumerable.Empty<Colis>();
			}

			return await context.Colis
				.Include(c => c.Client)
				.Include(c => c.Conteneur)
				.Include(c => c.Barcodes.Where(b => b.Actif))
				.Where(c => c.ClientId == user.ClientId.Value && c.Actif)
				.OrderByDescending(c => c.DateArrivee)
				.ToListAsync();
		}
		
    }
}