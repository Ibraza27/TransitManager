using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities; // Important
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{

    public class ConteneurService : IConteneurService
    {
        private readonly TransitContext _context;
        private readonly INotificationService _notificationService;

        public ConteneurService(TransitContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<Conteneur?> GetByIdAsync(Guid id)
        {
            return await _context.Conteneurs
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Client)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Conteneur?> GetByNumeroDossierAsync(string numeroDossier)
        {
            return await _context.Conteneurs
                .Include(c => c.Colis)
                    .ThenInclude(col => col.Client)
                .FirstOrDefaultAsync(c => c.NumeroDossier == numeroDossier);
        }

        public async Task<IEnumerable<Conteneur>> GetAllAsync()
        {
            return await _context.Conteneurs
                .Include(c => c.Colis)
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetActiveAsync()
        {
            return await _context.Conteneurs
                .Include(c => c.Colis)
                .Where(c => c.Statut != StatutConteneur.Cloture && 
                           c.Statut != StatutConteneur.Annule)
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetByDestinationAsync(string destination)
        {
            return await _context.Conteneurs
                .Include(c => c.Colis)
                .Where(c => c.Destination.Contains(destination) || 
                           c.PaysDestination.Contains(destination))
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetByStatusAsync(StatutConteneur statut)
        {
            return await _context.Conteneurs
                .Include(c => c.Colis)
                .Where(c => c.Statut == statut)
                .OrderByDescending(c => c.DateOuverture)
                .ToListAsync();
        }

        public async Task<Conteneur> CreateAsync(Conteneur conteneur)
        {
            // Validation de base
            if (string.IsNullOrEmpty(conteneur.Destination))
            {
                throw new InvalidOperationException("La destination est obligatoire.");
            }

            // Générer le numéro de dossier s'il n'est pas défini
            if (string.IsNullOrEmpty(conteneur.NumeroDossier))
            {
                conteneur.NumeroDossier = await GenerateUniqueDossierNumberAsync();
            }

            // Définir les valeurs par défaut
            conteneur.Statut = StatutConteneur.Ouvert;
            conteneur.DateOuverture = DateTime.UtcNow;

            _context.Conteneurs.Add(conteneur);
            await _context.SaveChangesAsync();

            // Notification
            await _notificationService.NotifyAsync(
                "Nouveau conteneur",
                $"Le conteneur {conteneur.NumeroDossier} pour {conteneur.Destination} a été créé.",
                TypeNotification.Information
            );

            return conteneur;
        }

        public async Task<Conteneur> UpdateAsync(Conteneur conteneur)
        {
            // Vérifier les changements de statut
            var original = await _context.Conteneurs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == conteneur.Id);

            if (original != null && original.Statut != conteneur.Statut)
            {
                await HandleStatusChangeAsync(conteneur, original.Statut, conteneur.Statut);
            }

            _context.Conteneurs.Update(conteneur);
            await _context.SaveChangesAsync();

            return conteneur;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var conteneur = await GetByIdAsync(id);
            if (conteneur == null) return false;

            // Vérifier si le conteneur peut être supprimé
            if (conteneur.Colis.Any())
            {
                throw new InvalidOperationException("Impossible de supprimer un conteneur contenant des colis.");
            }

            if (conteneur.Statut == StatutConteneur.EnTransit || 
                conteneur.Statut == StatutConteneur.Arrive)
            {
                throw new InvalidOperationException("Impossible de supprimer un conteneur en transit ou arrivé.");
            }

            // Suppression logique
            conteneur.Actif = false;
            conteneur.Statut = StatutConteneur.Annule;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> CloseConteneurAsync(Guid id)
        {
            var conteneur = await GetByIdAsync(id);
            if (conteneur == null) return false;

            if (conteneur.Statut != StatutConteneur.Livre)
            {
                throw new InvalidOperationException("Un conteneur doit être livré avant d'être clôturé.");
            }

            conteneur.Statut = StatutConteneur.Cloture;
            conteneur.DateCloture = DateTime.UtcNow;

            // Calculer la rentabilité finale
            var profitability = await CalculateProfitabilityAsync(id);

            await UpdateAsync(conteneur);

            // Notification
            await _notificationService.NotifyAsync(
                "Conteneur clôturé",
                $"Le conteneur {conteneur.NumeroDossier} a été clôturé. Rentabilité: {profitability:C}",
                TypeNotification.Succes
            );

            return true;
        }

        public async Task<bool> SetDepartureAsync(Guid id, DateTime departureDate)
        {
            var conteneur = await GetByIdAsync(id);
            if (conteneur == null) return false;

            conteneur.DateDepartReelle = departureDate;
            conteneur.Statut = StatutConteneur.EnTransit;

            // Mettre à jour le statut de tous les colis
            foreach (var colis in conteneur.Colis)
            {
                colis.Statut = StatutColis.EnTransit;
            }

            await UpdateAsync(conteneur);

            // Notification
            await _notificationService.NotifyAsync(
                "Conteneur parti",
                $"Le conteneur {conteneur.NumeroDossier} est parti pour {conteneur.Destination}.",
                TypeNotification.ConteneurPret
            );

            return true;
        }

        public async Task<bool> SetArrivalAsync(Guid id, DateTime arrivalDate)
        {
            var conteneur = await GetByIdAsync(id);
            if (conteneur == null) return false;

            conteneur.DateArriveeReelle = arrivalDate;
            conteneur.Statut = StatutConteneur.Arrive;

            // Mettre à jour le statut de tous les colis
            foreach (var colis in conteneur.Colis)
            {
                colis.Statut = StatutColis.Arrive;
            }

            await UpdateAsync(conteneur);

            // Notification pour tous les clients concernés
            var clients = conteneur.Colis.Select(c => c.Client).Distinct();
            foreach (var client in clients.Where(c => c != null))
            {
                await _notificationService.NotifyAsync(
                    "Conteneur arrivé",
                    $"Le conteneur {conteneur.NumeroDossier} est arrivé à {conteneur.Destination}.",
                    TypeNotification.Information
                );
            }

            return true;
        }

        public async Task<int> GetActiveCountAsync()
        {
            return await _context.Conteneurs
                .CountAsync(c => c.Actif && 
                            c.Statut != StatutConteneur.Cloture && 
                            c.Statut != StatutConteneur.Annule);
        }

        public async Task<decimal> GetAverageFillingRateAsync()
        {
            var conteneurs = await GetActiveAsync();
            if (!conteneurs.Any()) return 0;

            var rates = conteneurs.Select(c => (c.TauxRemplissageVolume + c.TauxRemplissagePoids) / 2);
            return rates.Average();
        }

        public async Task<IEnumerable<Conteneur>> GetUpcomingDeparturesAsync(int days)
        {
            var dateLimit = DateTime.UtcNow.AddDays(days);

            return await _context.Conteneurs
                .Include(c => c.Colis)
                .Where(c => c.Statut == StatutConteneur.Ouvert || 
                           c.Statut == StatutConteneur.EnPreparation)
                .Where(c => c.DateDepartPrevue != null && 
                           c.DateDepartPrevue <= dateLimit)
                .OrderBy(c => c.DateDepartPrevue)
                .ToListAsync();
        }

        public async Task<IEnumerable<Conteneur>> GetAlmostFullContainersAsync(decimal threshold)
        {
            var conteneurs = await GetActiveAsync();
            
            return conteneurs.Where(c => 
                c.TauxRemplissageVolume >= threshold || 
                c.TauxRemplissagePoids >= threshold);
        }

        public async Task<Dictionary<string, int>> GetStatsByDestinationAsync()
        {
            return await _context.Conteneurs
                .Where(c => c.Actif)
                .GroupBy(c => c.PaysDestination)
                .Select(g => new { Destination = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Destination, x => x.Count);
        }

        public async Task<bool> CanAddColisAsync(Guid conteneurId, Guid colisId)
        {
            var conteneur = await GetByIdAsync(conteneurId);
            if (conteneur == null) return false;

            var colis = await _context.Colis.FindAsync(colisId);
            if (colis == null) return false;

            // Vérifier l'espace disponible
            var newVolumeTotal = conteneur.VolumeUtilise + colis.Volume;
            var newPoidsTotal = conteneur.PoidsUtilise + colis.Poids;

            return conteneur.PeutRecevoirColis &&
                   newVolumeTotal <= conteneur.CapaciteVolume &&
                   newPoidsTotal <= conteneur.CapacitePoids;
        }

        public async Task<decimal> CalculateProfitabilityAsync(Guid conteneurId)
        {
            var conteneur = await GetByIdAsync(conteneurId);
            if (conteneur == null) return 0;

            // Calculer les revenus (somme des frais de transport des colis)
            var revenus = 0m;
            foreach (var colis in conteneur.Colis)
            {
                // Calculer le tarif basé sur le poids facturable
                var tarif = colis.PoidsFacturable * 2.5m; // Exemple: 2.5€/kg
                revenus += tarif;
            }

            // Soustraire les coûts
            var couts = conteneur.CoutTotal;

            return revenus - couts;
        }

        private async Task<string> GenerateUniqueDossierNumberAsync()
        {
            string numero;
            do
            {
                var year = DateTime.Now.ToString("yyyy");
                var month = DateTime.Now.ToString("MM");
                var random = new Random().Next(1000, 9999);
                numero = $"CONT-{year}{month}-{random}";
            }
            while (await _context.Conteneurs.AnyAsync(c => c.NumeroDossier == numero));

            return numero;
        }

        private async Task HandleStatusChangeAsync(Conteneur conteneur, StatutConteneur ancienStatut, StatutConteneur nouveauStatut)
        {
            // Logique spécifique lors des changements de statut
            if (nouveauStatut == StatutConteneur.EnTransit && ancienStatut != StatutConteneur.EnTransit)
            {
                // Notifier le départ
                await _notificationService.NotifyAsync(
                    "Conteneur en transit",
                    $"Le conteneur {conteneur.NumeroDossier} est maintenant en transit vers {conteneur.Destination}.",
                    TypeNotification.Information
                );
            }

            await Task.CompletedTask;
        }

        public Task<IEnumerable<string>> GetAllDestinationsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Conteneur>> GetOpenConteneursAsync()
        {
            throw new NotImplementedException();
        }
    }
}