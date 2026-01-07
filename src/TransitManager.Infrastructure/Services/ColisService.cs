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
using TransitManager.Infrastructure.Data.Uow;
using TransitManager.Infrastructure.Repositories;

namespace TransitManager.Infrastructure.Services
{
    public class ColisService : IColisService
    {
        // Remplacer IDbContextFactory par IUnitOfWorkFactory
        private readonly IUnitOfWorkFactory _uowFactory;

        private readonly INotificationService _notificationService;
        private readonly IConteneurService _conteneurService;
        private readonly IClientService _clientService;
        private readonly ITimelineService _timelineService;
        private readonly IAuthenticationService _authService;

        // Définition centralisée des statuts qui ne doivent PAS être écrasés par le conteneur
        private readonly StatutColis[] _statutsCritiques = new[]
        {
            StatutColis.Livre,
            StatutColis.Perdu,
            StatutColis.Retourne,
            StatutColis.Probleme
        };

        public ColisService(
            IUnitOfWorkFactory uowFactory, // <-- MODIFIÉ
            INotificationService notificationService,
            IConteneurService conteneurService,
            IClientService clientService,
            ITimelineService timelineService,
            IAuthenticationService authService)
        {
            _uowFactory = uowFactory; // <-- MODIFIÉ
            _notificationService = notificationService;
            _conteneurService = conteneurService;
            _clientService = clientService;
            _timelineService = timelineService;
            _authService = authService;
        }

        public async Task RecalculateAndUpdateColisStatisticsAsync(Guid colisId)
        {
            using var uow = await _uowFactory.CreateAsync();

            // 1. On récupère le colis
            var colis = await uow.Colis.GetByIdAsync(colisId);

            if (colis != null)
            {
                // 2. On récupère les paiements via le Repository de l'UoW
                var paiements = await uow.Paiements.GetByColisAsync(colisId);

                // 3. On calcule la somme (uniquement les paiements valides/payés)
                var totalPaye = paiements
                    .Where(p => p.Actif && (p.Statut == StatutPaiement.Paye || p.Statut == StatutPaiement.Valide))
                    .Sum(p => p.Montant);

                // 4. On met à jour le colis
                colis.SommePayee = totalPaye;

                // 5. On sauvegarde
                await uow.CommitAsync();
            }
        }

        public async Task<Colis> CreateAsync(CreateColisDto colisDto)
        {
            using var uow = await _uowFactory.CreateAsync();
            var newColis = new Colis
            {
                ClientId = colisDto.ClientId,
                Designation = colisDto.Designation,
                DestinationFinale = colisDto.DestinationFinale,
                NombrePieces = colisDto.NombrePieces,
                Volume = colisDto.Volume,
                ValeurDeclaree = colisDto.ValeurDeclaree,
                PrixTotal = colisDto.PrixTotal,
                FraisDouane = colisDto.FraisDouane, // MAPPING
                Destinataire = colisDto.Destinataire,
                TelephoneDestinataire = colisDto.TelephoneDestinataire,
                LivraisonADomicile = colisDto.LivraisonADomicile,
                AdresseLivraison = colisDto.AdresseLivraison,
                EstFragile = colisDto.EstFragile,
                ManipulationSpeciale = colisDto.ManipulationSpeciale,
                InstructionsSpeciales = colisDto.InstructionsSpeciales,
                Type = colisDto.Type,
                TypeEnvoi = colisDto.TypeEnvoi,
                ConteneurId = colisDto.ConteneurId,
                AdresseFrance = colisDto.AdresseFrance,
                AdresseDestination = colisDto.AdresseDestination,
                // MAPPING CORRIGÉ : Inventaire et Signatures
                InventaireJson = colisDto.InventaireJson,
                LieuSignatureInventaire = colisDto.LieuSignatureInventaire,
                DateSignatureInventaire = colisDto.DateSignatureInventaire,
                SignatureClientInventaire = colisDto.SignatureClientInventaire
            };
            foreach (var barcodeValue in colisDto.Barcodes)
            {
                newColis.Barcodes.Add(new Barcode { Value = barcodeValue });
            }

            // Si un conteneur est assigné à la création, on applique déjà la logique de statut
            if (newColis.ConteneurId.HasValue)
            {
                var conteneur = await uow.Conteneurs.GetByIdAsync(newColis.ConteneurId.Value);
                if (conteneur != null)
                {
                    newColis.Statut = GetStatutFromConteneur(conteneur.Statut);
                    newColis.NumeroPlomb = conteneur.NumeroPlomb;
                }
            }

            await uow.Colis.AddAsync(newColis);
            await uow.CommitAsync();
            await _timelineService.AddEventAsync("Colis créé et enregistré", colisId: newColis.Id, statut: newColis.Statut.ToString());

            var client = await uow.Clients.GetByIdAsync(newColis.ClientId);
            
            // NOTIFICATION ADMIN
            await _notificationService.CreateAndSendAsync(
                title: "📦 Nouveau Colis",
                message: $"Nouveau colis {newColis.NumeroReference} ajouté pour {client?.NomComplet ?? "Client inconnu"}",
                userId: null, // Broadcast Admin
                categorie: CategorieNotification.StatutColis,
                actionUrl: $"/colis/edit/{newColis.Id}", // URL Admin
                relatedEntityId: newColis.Id,
                relatedEntityType: "Colis"
            );

            var clientUser = await uow.Utilisateurs.GetByClientIdAsync(newColis.ClientId);
            if (clientUser != null)
            {
                await _notificationService.CreateAndSendAsync(
                    title: "📦 Nouveau Colis",
                    message: $"Un nouveau colis ({newColis.Designation}) a été créé pour vous.",
                    userId: newColis.ClientId,
                    categorie: CategorieNotification.StatutColis,
                    actionUrl: $"/colis/detail/{newColis.Id}",
                    relatedEntityId: newColis.Id,
                    relatedEntityType: "Colis"
                );
            }

            await _clientService.RecalculateAndUpdateClientStatisticsAsync(newColis.ClientId);
            if (newColis.ConteneurId.HasValue) await _conteneurService.RecalculateStatusAsync(newColis.ConteneurId.Value);
            return newColis;
        }

        public async Task<Colis> UpdateAsync(Guid id, UpdateColisDto colisDto)
        {
            using var uow = await _uowFactory.CreateAsync();
            try
            {
                var colisInDb = await uow.Colis.GetByIdAsync(id);
                if (colisInDb == null) throw new InvalidOperationException("Le colis n'existe plus.");
                var originalClientId = colisInDb.ClientId;
                var originalConteneurId = colisInDb.ConteneurId;
                var oldStatus = colisInDb.Statut;
                
                Console.WriteLine($"[DEBUG] UpdateColisAsync: ID={id}, DTO Volume={colisDto.Volume}, DB Volume={colisInDb.Volume}");    
                Console.WriteLine($"[DEBUG] InventaireJson Length: {colisInDb.InventaireJson?.Length ?? 0}");

                // --- 1. Logique "Retourné" ---
                if (colisDto.Statut == StatutColis.Retourne)
                {
                    colisDto.ConteneurId = null;
                }

                // --- 2. Mapping des champs simples ---
                colisInDb.ClientId = colisDto.ClientId;
                colisInDb.Designation = colisDto.Designation;
                colisInDb.DestinationFinale = colisDto.DestinationFinale;
                if (string.IsNullOrEmpty(colisInDb.InventaireJson) || colisInDb.InventaireJson == "[]")
                {
                    colisInDb.NombrePieces = colisDto.NombrePieces;
                    colisInDb.ValeurDeclaree = colisDto.ValeurDeclaree;
                }
                colisInDb.Volume = colisDto.Volume; // Moved out of if block
                colisInDb.PrixTotal = colisDto.PrixTotal;
                colisInDb.FraisDouane = colisDto.FraisDouane; // AJOUT
                colisInDb.Destinataire = colisDto.Destinataire;
                colisInDb.TelephoneDestinataire = colisDto.TelephoneDestinataire;
                colisInDb.LivraisonADomicile = colisDto.LivraisonADomicile;
                colisInDb.AdresseLivraison = colisDto.AdresseLivraison;
                colisInDb.AdresseFrance = colisDto.AdresseFrance;
                colisInDb.AdresseDestination = colisDto.AdresseDestination;
                colisInDb.EstFragile = colisDto.EstFragile;
                colisInDb.ManipulationSpeciale = colisDto.ManipulationSpeciale;
                colisInDb.InstructionsSpeciales = colisDto.InstructionsSpeciales;
                colisInDb.Type = colisDto.Type;
                colisInDb.TypeEnvoi = colisDto.TypeEnvoi;
                colisInDb.ConteneurId = colisDto.ConteneurId;

                // Mises à jour inventaire/signatures
                colisInDb.LieuSignatureInventaire = colisDto.LieuSignatureInventaire;
                colisInDb.DateSignatureInventaire = colisDto.DateSignatureInventaire;
                colisInDb.SignatureClientInventaire = colisDto.SignatureClientInventaire;
                if(colisDto.InventaireJson != null) colisInDb.InventaireJson = colisDto.InventaireJson; // Important si mis à jour ici

                // --- 3. LOGIQUE INTELLIGENTE DU STATUT ---
                // Si l'utilisateur a choisi manuellement un statut critique, on le respecte impérativement
                if (_statutsCritiques.Contains(colisDto.Statut))
                {
                    colisInDb.Statut = colisDto.Statut;
                }
                // Sinon, si le colis est dans un conteneur, le conteneur DICCTE le statut
                else if (colisInDb.ConteneurId.HasValue)
                {
                    // On charge le conteneur pour connaître son statut actuel
                    var conteneur = await uow.Conteneurs.GetByIdAsync(colisInDb.ConteneurId.Value);
                    if (conteneur != null)
                    {
                        colisInDb.Statut = GetStatutFromConteneur(conteneur.Statut);
                        colisInDb.NumeroPlomb = conteneur.NumeroPlomb;
                    }
                    else
                    {
                        // Fallback si conteneur introuvable (ne devrait pas arriver)
                        colisInDb.Statut = colisDto.Statut;
                    }
                }
                else
                {
                    // Pas de conteneur, pas de statut critique : on prend ce que l'UI envoie (ex: EnAttente)
                    colisInDb.Statut = colisDto.Statut;
                    colisInDb.NumeroPlomb = null;
                }

                await uow.Colis.UpdateAsync(colisInDb);

                // --- 4. Gestion des Codes-Barres (Inchangé) ---
                var barcodesFromUIValues = colisDto.Barcodes.ToHashSet();
                var barcodesInDb = colisInDb.Barcodes.ToList();
                var barcodesToRemove = barcodesInDb.Where(b => !barcodesFromUIValues.Contains(b.Value)).ToList();
                if (barcodesToRemove.Any()) await uow.Barcodes.RemoveRangeAsync(barcodesToRemove);
                var barcodesInDbValues = barcodesInDb.Select(b => b.Value).ToHashSet();
                var barcodeValuesToAdd = barcodesFromUIValues.Where(uiValue => !barcodesInDbValues.Contains(uiValue));
                foreach (var valueToAdd in barcodeValuesToAdd) uow.Barcodes.AddAsync(new Barcode { Value = valueToAdd, ColisId = colisInDb.Id });

                await uow.CommitAsync();

                // --- 5. Timeline ---
                if (oldStatus != colisInDb.Statut)
                {
                    await _timelineService.AddEventAsync(
                        $"Statut modifié : {oldStatus} -> {colisInDb.Statut}",
                        colisId: colisInDb.Id,
                        statut: colisInDb.Statut.ToString()
                    );

                    // NOTIFICATION CLIENT
                    // On notifie le client si ce n'est pas un statut interne ou si le user a un compte
                    var clientUser = await uow.Utilisateurs.GetByClientIdAsync(colisInDb.ClientId);
                    if (clientUser != null)
                    {
                        string emoji = colisInDb.Statut == StatutColis.Livre ? "✅" : "📦";
                        await _notificationService.CreateAndSendAsync(
                            title: $"{emoji} Mise à jour Colis",
                            message: $"Votre colis {colisInDb.NumeroReference} est maintenant : {colisInDb.Statut}",
                            userId: clientUser.Id,
                            categorie: CategorieNotification.StatutColis,
                            actionUrl: $"/colis/detail/{colisInDb.Id}?tab=suivi",
                            relatedEntityId: colisInDb.Id,
                            relatedEntityType: "Colis"
                        );
                    }
                }

                // --- 6. Appels Services Externes ---
                await _clientService.RecalculateAndUpdateClientStatisticsAsync(colisInDb.ClientId);
                if (originalClientId != colisInDb.ClientId) await _clientService.RecalculateAndUpdateClientStatisticsAsync(originalClientId);
                if (originalConteneurId.HasValue && originalConteneurId.Value != colisInDb.ConteneurId) await _conteneurService.RecalculateStatusAsync(originalConteneurId.Value);
                if (colisInDb.ConteneurId.HasValue) await _conteneurService.RecalculateStatusAsync(colisInDb.ConteneurId.Value);
                return colisInDb;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERREUR ColisService.UpdateAsync] : {ex.Message}");
                throw;
            }
        }

        public async Task<bool> AssignToConteneurAsync(Guid colisId, Guid conteneurId)
        {
            using var uow = await _uowFactory.CreateAsync();
            var colis = await uow.Colis.GetByIdAsync(colisId);
            var conteneur = await uow.Conteneurs.GetByIdAsync(conteneurId);

            // On autorise l'ajout même si le conteneur est parti, sauf s'il est clos
            var blockedStatuses = new[] { StatutConteneur.Cloture, StatutConteneur.Annule };
            if (colis == null || conteneur == null || blockedStatuses.Contains(conteneur.Statut)) return false;
            var oldStatus = colis.Statut;
            colis.ConteneurId = conteneurId;
            colis.NumeroPlomb = conteneur.NumeroPlomb;

            // --- LOGIQUE HÉRITAGE FORCÉ ---
            // Si le colis n'est pas dans un état critique (Livre/Perdu/Retourne/Probleme)
            if (!_statutsCritiques.Contains(colis.Statut))
            {
                // Il prend OBLIGATOIREMENT le statut correspondant à celui du conteneur
                colis.Statut = GetStatutFromConteneur(conteneur.Statut);
            }

            await uow.CommitAsync();

            // Timeline
            if (oldStatus != colis.Statut)
            {
                await _timelineService.AddEventAsync(
                    $"Mise à jour automatique (Conteneur {conteneur.Statut}) : {oldStatus} -> {colis.Statut}",
                    colisId: colis.Id,
                    statut: colis.Statut.ToString()
                );
            }
            await _timelineService.AddEventAsync(
                $"Affecté au conteneur {conteneur.NumeroDossier}",
                colisId: colis.Id,
                conteneurId: conteneur.Id,
                location: conteneur.NomCompagnie,
                statut: colis.Statut.ToString()
            );

            await _conteneurService.RecalculateStatusAsync(conteneurId);
            return true;
        }

        public async Task<bool> RemoveFromConteneurAsync(Guid colisId)
        {
            using var uow = await _uowFactory.CreateAsync();
            var colis = await uow.Colis.GetByIdAsync(colisId);

            if (colis == null || !colis.ConteneurId.HasValue) return false;
            var originalConteneurId = colis.ConteneurId.Value;
            var oldStatus = colis.Statut;
            colis.ConteneurId = null;
            colis.NumeroPlomb = null;

            // Si le statut n'était pas critique, il redevient "En Attente"
            if (!_statutsCritiques.Contains(colis.Statut))
            {
                colis.Statut = StatutColis.EnAttente;
            }

            await uow.CommitAsync();
            if (oldStatus != colis.Statut)
            {
                await _timelineService.AddEventAsync($"Retrait conteneur : {oldStatus} -> {colis.Statut}", colisId: colis.Id, statut: colis.Statut.ToString());
            }

            await _conteneurService.RecalculateStatusAsync(originalConteneurId);
            return true;
        }

        // --- MÉTHODE UTILITAIRE PRIVÉE ---
        private StatutColis GetStatutFromConteneur(StatutConteneur statutConteneur)
        {
            return statutConteneur switch
            {
                StatutConteneur.EnTransit => StatutColis.EnTransit,
                StatutConteneur.Arrive => StatutColis.Arrive,
                StatutConteneur.EnDedouanement => StatutColis.EnDedouanement,
                StatutConteneur.Livre => StatutColis.Livre,
                // Pour Reçu, EnPreparation, Problème(conteneur), Ouvert -> On reste sur Affecté
                _ => StatutColis.Affecte
            };
        }

        public async Task UpdateInventaireAsync(UpdateInventaireDto dto)
        {
            using var uow = await _uowFactory.CreateAsync();

            // On récupère le colis
            var colis = await uow.Colis.GetByIdAsync(dto.ColisId);

            if (colis != null)
            {
                // 1. Mise à jour des données
                colis.InventaireJson = dto.InventaireJson;
                colis.NombrePieces = dto.TotalPieces;
                colis.ValeurDeclaree = dto.TotalValeurDeclaree;
                colis.LieuSignatureInventaire = dto.LieuSignatureInventaire;
                colis.DateSignatureInventaire = dto.DateSignatureInventaire;
                colis.SignatureClientInventaire = dto.SignatureClientInventaire;

                await uow.Colis.UpdateAsync(colis);
                await uow.CommitAsync();

                // 2. Timeline
                await _timelineService.AddEventAsync("Inventaire mis à jour", colisId: colis.Id);
                await _clientService.RecalculateAndUpdateClientStatisticsAsync(colis.ClientId);

                // === AJOUT : NOTIFICATIONS ===

                // A. Notifier les Administrateurs
                await _notificationService.CreateAndSendAsync(
                    title: "📋 Inventaire Modifié",
                    message: $"L'inventaire du colis {colis.NumeroReference} a été mis à jour ({colis.NombrePieces} pces).",
                    userId: null, // null = Broadcast aux Admins
                    categorie: CategorieNotification.Inventaire,
                    actionUrl: $"/colis/edit/{colis.Id}",
                    relatedEntityId: colis.Id,
                    relatedEntityType: "Colis"
                );

                // B. Notifier le Client (s'il a un compte utilisateur)
                // On cherche l'utilisateur lié à ce client
                var clientUser = await uow.Utilisateurs.GetByClientIdAsync(colis.ClientId);

                if (clientUser != null)
                {
                    await _notificationService.CreateAndSendAsync(
                        title: "📋 Inventaire Validé",
                        message: $"L'inventaire de votre colis {colis.NumeroReference} a été mis à jour et sauvegardé.",
                        userId: clientUser.Id, // ID du client
                        categorie: CategorieNotification.Inventaire,
                        actionUrl: $"/colis/edit/{colis.Id}",
                        relatedEntityId: colis.Id,
                        relatedEntityType: "Colis"
                    );
                }
                // =============================
            }
        }

        public async Task<Colis?> GetByIdAsync(Guid id)
        {
            using var uow = await _uowFactory.CreateAsync();
            // Utilise la méthode spécifique du repo qui inclut les détails (Client, Barcodes...)
            return await uow.Colis.GetWithDetailsAsync(id);
        }

        public async Task<Colis?> GetByBarcodeAsync(string barcode)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Colis.GetByBarcodeAsync(barcode);
        }

        public async Task<Colis?> GetByReferenceAsync(string reference)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Colis.GetByReferenceAsync(reference);
        }

        public async Task<IEnumerable<Colis>> GetAllAsync()
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Colis.GetAllAsync();
        }

        public async Task<IEnumerable<Colis>> GetByClientAsync(Guid clientId)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Colis.GetByClientAsync(clientId);
        }

        public async Task<IEnumerable<Colis>> GetByConteneurAsync(Guid conteneurId)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Colis.GetByConteneurAsync(conteneurId);
        }

        public async Task<IEnumerable<Colis>> GetByStatusAsync(StatutColis statut)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Colis.GetByStatusAsync(statut);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            using var uow = await _uowFactory.CreateAsync();
            var colis = await uow.Colis.GetByIdAsync(id);
            if (colis == null) return false;
            var clientId = colis.ClientId;
            colis.Actif = false;
            await uow.Colis.UpdateAsync(colis);
            await uow.CommitAsync();
            
            // CLEANUP NOTIFICATIONS
            await _notificationService.DeleteByEntityAsync(id, CategorieNotification.StatutColis);
            
            await _clientService.RecalculateAndUpdateClientStatisticsAsync(clientId);
            return true;
        }

        public async Task<Colis> ScanAsync(string barcode, string location)
        {
            using var uow = await _uowFactory.CreateAsync();
            var colis = await uow.Colis.GetByBarcodeAsync(barcode);
            if (colis == null) throw new InvalidOperationException($"Colis introuvable avec le code-barres {barcode}");
            var history = string.IsNullOrEmpty(colis.HistoriqueScan) ? new List<object>() : JsonSerializer.Deserialize<List<object>>(colis.HistoriqueScan) ?? new List<object>();
            history.Add(new { Date = DateTime.UtcNow, Location = location, Status = colis.Statut.ToString() });
            colis.HistoriqueScan = JsonSerializer.Serialize(history);
            colis.DateDernierScan = DateTime.UtcNow;
            colis.LocalisationActuelle = location;
            await uow.Colis.UpdateAsync(colis);
            await uow.CommitAsync();

            await _timelineService.AddEventAsync("Scan effectué", colisId: colis.Id, location: location);
            return colis;
        }

        public async Task<int> GetCountByStatusAsync(StatutColis statut)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Colis.GetCountByStatusAsync(statut);
        }

        public async Task<IEnumerable<Colis>> GetRecentColisAsync(int count)
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Colis.GetRecentAsync(count);
        }

        public async Task<IEnumerable<Colis>> GetColisWaitingLongTimeAsync(int days)
        {
            using var uow = await _uowFactory.CreateAsync();
            var dateLimit = DateTime.UtcNow.AddDays(-days);
            return await uow.Colis.GetColisWaitingLongTimeAsync(days);
        }

        public async Task<bool> MarkAsDeliveredAsync(Guid colisId, string signature)
        {
            using var uow = await _uowFactory.CreateAsync();
            var colis = await uow.Colis.GetByIdAsync(colisId);
            if (colis == null) return false;
            colis.Statut = StatutColis.Livre;
            colis.DateLivraison = DateTime.UtcNow;
            colis.SignatureReception = signature;
            await uow.Colis.UpdateAsync(colis);
            await uow.CommitAsync();
            await _timelineService.AddEventAsync("Livré au client", colisId: colis.Id, statut: "Livré");
            return true;
        }

        public async Task<IEnumerable<Colis>> SearchAsync(string searchTerm)
        {
            using var uow = await _uowFactory.CreateAsync();
            if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllAsync();
            return await uow.Colis.SearchAsync(searchTerm);
        }

        public async Task<Dictionary<StatutColis, int>> GetStatisticsByStatusAsync()
        {
            using var uow = await _uowFactory.CreateAsync();
            return await uow.Colis.GetStatisticsByStatusAsync();
        }

        public async Task<IEnumerable<Colis>> GetByUserIdAsync(Guid userId)
        {
            using var uow = await _uowFactory.CreateAsync();
            var user = await uow.Utilisateurs.GetByIdAsync(userId);
            if (user == null || !user.ClientId.HasValue)
            {
                return Enumerable.Empty<Colis>();
            }
            return await uow.Colis.GetByClientAsync(user.ClientId.Value);
        }

        public async Task<PagedResult<ColisListItemDto>> GetPagedAsync(int page, int pageSize, string? search = null, Guid? clientId = null)
        {
            using var uow = await _uowFactory.CreateAsync();

            var query = await uow.Colis.GetPagedAsync(page, pageSize, search, clientId);

            return query;
        }


        public async Task<Dictionary<string, decimal>> GetMonthlyVolumeAsync(int months)
        {
             // On ne peut pas utiliser uow directement pour des requêtes custom GroupBy complexes sur DbContext si uow n'expose pas le DbSet directement de manière compatible.
             // On utilise contextFactory si dispo, sinon uow.
             // ColisService a uowFactory... Il faut voir si on peut accès au context.
             // ColisRepository a le context.
             // Hack : on utilise uow.Colis.GetContext() si possible ? Non.
             // On va simuler ou ajouter ça au repo idéalement, mais ici on va faire simple.
             // uowFactory.CreateAsync() -> uow.
             // Pas d'accès direct au DbContext ici (c'est un UoW pattern).
             // Il faut ajouter la méthode au Repository ColisRepository.
             // Pour aller plus vite, on va récupérer tous les colis (si pas trop nombreux) et filtrer en mémoire, C'EST SALE mais direct.
             // Ou mieux : Ajoutons à IColisRepository.
             
             // ... Wait, ColisService construction : public ColisService(IUnitOfWorkFactory uowFactory...)
             
             // PLAN B (Lazy) : In-Memory filtering (Ok pour < 10000 colis)
             using var uow = await _uowFactory.CreateAsync();
             var allColis = await uow.Colis.GetAllAsync(); // AIE, GetAll retourne tout ?
             
             var limitDate = DateTime.UtcNow.AddMonths(-months);
             var filtered = allColis.Where(c => c.DateCreation >= limitDate && c.Actif);
             
             var data = filtered.GroupBy(c => new { c.DateCreation.Year, c.DateCreation.Month })
                .Select(g => new { 
                    Year = g.Key.Year, 
                    Month = g.Key.Month, 
                    Volume = g.Sum(x => x.Volume) 
                })
                .ToList();

            var result = new Dictionary<string, decimal>();
            for (int i = 0; i < months; i++)
            {
                var d = DateTime.UtcNow.AddMonths(-i);
                var key = d.ToString("MMM yyyy"); 
                var entry = data.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month);
                result[key] = entry?.Volume ?? 0;
            }
            return result;
        }

        public async Task<IEnumerable<Colis>> GetUnpricedColisAsync()
        {
             using var uow = await _uowFactory.CreateAsync();
             var all = await uow.Colis.GetAllAsync(); // Ideal would be dedicated repo method, but efficient enough for now if not huge
             // Or better: add to IColisRepository. But to keep it simple and consistent with previous patterns:
             // Wait, I can use a predicate on GetAll? No, GetAll returns IEnumerable?
             // ColisRepository.GetAllAsync returns List.
             
             // Optimal: Add to Repository. 
             // Quick: Filter in memory.
             // Given constraint of modifying many files, I will stick to repo pattern where I can, or memory if easy.
             // Actually, I can use context if I really wanted to but UoW pattern hides it.
             // Use GetAll and filter.
             return all.Where(c => c.Actif && c.PrixTotal == 0).OrderByDescending(c => c.DateCreation).ToList();
        }

        public async Task<bool> SetExportExclusionAsync(Guid id, bool isExcluded)
        {
            using var uow = await _uowFactory.CreateAsync();
            var colis = await uow.Colis.GetByIdAsync(id);
            if (colis == null) return false;

            colis.IsExcludedFromExport = isExcluded;
            await uow.Colis.UpdateAsync(colis);
            await uow.CommitAsync();
            return true;
        }

    }
}
