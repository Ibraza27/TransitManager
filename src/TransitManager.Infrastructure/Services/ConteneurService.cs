using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class ConteneurService : IConteneurService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly INotificationService _notificationService;

        public ConteneurService(IDbContextFactory<TransitContext> contextFactory, INotificationService notificationService)
        {
            _contextFactory = contextFactory;
            _notificationService = notificationService;
        }

        public async Task<Conteneur?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis)
                .ThenInclude(col => col.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Conteneur?> GetByNumeroDossierAsync(string numeroDossier)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis)
                .ThenInclude(col => col.Client)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.NumeroDossier == numeroDossier);
        }

        public async Task<IEnumerable<Conteneur>> GetAllAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis)
                .AsNoTracking()
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetActiveAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis)
                .Where(c => c.Statut != StatutConteneur.Cloture && c.Statut != StatutConteneur.Annule)
                .AsNoTracking()
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetByDestinationAsync(string destination)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis)
                .Where(c => c.Destination.Contains(destination) || c.PaysDestination.Contains(destination))
                .AsNoTracking()
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetByStatusAsync(StatutConteneur statut)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis)
                .Where(c => c.Statut == statut)
                .AsNoTracking()
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<Conteneur> CreateAsync(Conteneur conteneur)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            if (string.IsNullOrEmpty(conteneur.Destination))
            {
                throw new InvalidOperationException("La destination est obligatoire.");
            }

            if (string.IsNullOrEmpty(conteneur.NumeroDossier))
            {
                conteneur.NumeroDossier = await GenerateUniqueDossierNumberAsync(context);
            }
            
            conteneur.Statut = StatutConteneur.Ouvert;
            conteneur.DateOuverture = DateTime.UtcNow;

            context.Conteneurs.Add(conteneur);
            await context.SaveChangesAsync();

            await _notificationService.NotifyAsync("Nouveau conteneur", $"Le conteneur {conteneur.NumeroDossier} pour {conteneur.Destination} a été créé.");
            return conteneur;
        }

        public async Task<Conteneur> UpdateAsync(Conteneur conteneur)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var original = await context.Conteneurs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == conteneur.Id);

            if (original != null && original.Statut != conteneur.Statut)
            {
                await HandleStatusChangeAsync(conteneur);
            }

            context.Conteneurs.Update(conteneur);
            await context.SaveChangesAsync();
            return conteneur;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var conteneur = await context.Conteneurs.Include(c => c.Colis).FirstOrDefaultAsync(c => c.Id == id);
            if (conteneur == null) return false;

            if (conteneur.Colis.Any())
            {
                throw new InvalidOperationException("Impossible de supprimer un conteneur contenant des colis.");
            }

            if (conteneur.Statut is StatutConteneur.EnTransit or StatutConteneur.Arrive)
            {
                throw new InvalidOperationException("Impossible de supprimer un conteneur en transit ou arrivé.");
            }

            conteneur.Actif = false;
            conteneur.Statut = StatutConteneur.Annule;
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CloseConteneurAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var conteneur = await context.Conteneurs.Include(c => c.Colis).FirstOrDefaultAsync(c => c.Id == id);
            if (conteneur == null) return false;

            if (conteneur.Statut != StatutConteneur.Livre)
            {
                throw new InvalidOperationException("Un conteneur doit être livré avant d'être clôturé.");
            }

            conteneur.Statut = StatutConteneur.Cloture;
            conteneur.DateCloture = DateTime.UtcNow;
            
            var profitability = CalculateProfitability(conteneur);
            
            await context.SaveChangesAsync();
            
            await _notificationService.NotifyAsync("Conteneur clôturé", $"Le conteneur {conteneur.NumeroDossier} a été clôturé. Rentabilité: {profitability:C}", Core.Enums.TypeNotification.Succes);
            return true;
        }

        public async Task<bool> SetDepartureAsync(Guid id, DateTime departureDate)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var conteneur = await context.Conteneurs.Include(c => c.Colis).FirstOrDefaultAsync(c => c.Id == id);
            if (conteneur == null) return false;

            conteneur.DateDepartReelle = departureDate;
            conteneur.Statut = StatutConteneur.EnTransit;

            foreach (var colis in conteneur.Colis)
            {
                colis.Statut = StatutColis.EnTransit;
            }

            await context.SaveChangesAsync();

            await _notificationService.NotifyAsync("Conteneur parti", $"Le conteneur {conteneur.NumeroDossier} est parti pour {conteneur.Destination}.");
            return true;
        }

        public async Task<bool> SetArrivalAsync(Guid id, DateTime arrivalDate)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var conteneur = await context.Conteneurs.Include(c => c.Colis).ThenInclude(co => co.Client).FirstOrDefaultAsync(c => c.Id == id);
            if (conteneur == null) return false;

            conteneur.DateArriveeReelle = arrivalDate;
            conteneur.Statut = StatutConteneur.Arrive;

            foreach (var colis in conteneur.Colis)
            {
                colis.Statut = StatutColis.Arrive;
            }

            await context.SaveChangesAsync();

            var clients = conteneur.Colis.Select(c => c.Client).Distinct();
            foreach (var client in clients.Where(c => c != null))
            {
                await _notificationService.NotifyAsync("Conteneur arrivé", $"Le conteneur {conteneur.NumeroDossier} est arrivé à {conteneur.Destination}.");
            }
            return true;
        }

        public async Task<int> GetActiveCountAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs.CountAsync(c => c.Actif && c.Statut != StatutConteneur.Cloture && c.Statut != StatutConteneur.Annule);
        }

        public async Task<decimal> GetAverageFillingRateAsync()
        {
            var conteneurs = await GetActiveAsync();
            if (!conteneurs.Any()) return 0;
            return conteneurs.Average(c => c.TauxRemplissageVolume);
        }

        public async Task<IEnumerable<Conteneur>> GetUpcomingDeparturesAsync(int days)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var dateLimit = DateTime.UtcNow.AddDays(days);
            return await context.Conteneurs
                .Include(c => c.Colis)
                .Where(c => c.Statut == StatutConteneur.Ouvert || c.Statut == StatutConteneur.EnPreparation)
                .Where(c => c.DateDepartPrevue != null && c.DateDepartPrevue <= dateLimit)
                .AsNoTracking()
                .OrderBy(c => c.DateDepartPrevue)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetAlmostFullContainersAsync(decimal threshold)
        {
            var activeContainers = await GetActiveAsync();
            return activeContainers.Where(c => c.TauxRemplissageVolume >= threshold || c.TauxRemplissagePoids >= threshold);
        }

        public async Task<Dictionary<string, int>> GetStatsByDestinationAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Where(c => c.Actif && !string.IsNullOrEmpty(c.PaysDestination))
                .GroupBy(c => c.PaysDestination)
                .Select(g => new { Destination = g.Key, Count = g.Count() })
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Destination, x => x.Count);
        }

        public async Task<bool> CanAddColisAsync(Guid conteneurId, Guid colisId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var conteneur = await context.Conteneurs.Include(c => c.Colis).FirstOrDefaultAsync(c => c.Id == conteneurId);
            var colis = await context.Colis.FindAsync(colisId);

            if (conteneur == null || colis == null) return false;

            var newVolumeTotal = conteneur.VolumeUtilise + colis.Volume;
            var newPoidsTotal = conteneur.PoidsUtilise + colis.Poids;

            return conteneur.PeutRecevoirColis && newVolumeTotal <= conteneur.CapaciteVolume && newPoidsTotal <= conteneur.CapacitePoids;
        }

        public async Task<decimal> CalculateProfitabilityAsync(Guid conteneurId)
        {
            var conteneur = await GetByIdAsync(conteneurId);
            if (conteneur == null) return 0;
            return CalculateProfitability(conteneur);
        }
        
        public decimal CalculateProfitability(Conteneur conteneur)
        {
            var revenus = conteneur.Colis.Sum(colis => colis.PoidsFacturable * 2.5m);
            return revenus - conteneur.CoutTotal;
        }
        
        public async Task<IEnumerable<string>> GetAllDestinationsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Where(c => c.Actif)
                .Select(c => c.Destination)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetOpenConteneursAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Conteneurs
                .Include(c => c.Colis)
                .Where(c => c.Actif && (c.Statut == StatutConteneur.Ouvert || c.Statut == StatutConteneur.EnPreparation))
                .AsNoTracking()
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        private async Task<string> GenerateUniqueDossierNumberAsync(TransitContext context)
        {
            string numero;
            do
            {
                numero = $"CONT-{DateTime.Now:yyyyMM}-{new Random().Next(1000, 9999)}";
            } while (await context.Conteneurs.AnyAsync(c => c.NumeroDossier == numero));
            return numero;
        }

        private async Task HandleStatusChangeAsync(Conteneur conteneur)
        {
            if (conteneur.Statut == StatutConteneur.EnTransit)
            {
                await _notificationService.NotifyAsync("Conteneur en transit", $"Le conteneur {conteneur.NumeroDossier} est maintenant en transit vers {conteneur.Destination}.");
            }
        }
    }
}