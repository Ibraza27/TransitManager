using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly string _storageRootPath;
        private readonly ITimelineService _timelineService;
        private readonly INotificationService _notificationService; // AJOUT

        public DocumentService(
            IDbContextFactory<TransitContext> contextFactory,
            IConfiguration configuration,
            ITimelineService timelineService,
            INotificationService notificationService) // AJOUT
        {
            _contextFactory = contextFactory;
            _timelineService = timelineService;
            _notificationService = notificationService; // AJOUT

            _storageRootPath = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "Storage");
            if (!Directory.Exists(_storageRootPath))
            {
                Directory.CreateDirectory(_storageRootPath);
            }
        }

        public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, TypeDocument typeDoc, Guid? clientId = null, Guid? colisId = null, Guid? vehiculeId = null, Guid? conteneurId = null, bool estConfidentiel = false)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            string subFolder = "Misc";
            string entityIdFolder = Guid.NewGuid().ToString();

            if (clientId.HasValue) { subFolder = "Clients"; entityIdFolder = clientId.Value.ToString(); }
            else if (vehiculeId.HasValue) { subFolder = "Vehicules"; entityIdFolder = vehiculeId.Value.ToString(); }
            else if (colisId.HasValue) { subFolder = "Colis"; entityIdFolder = colisId.Value.ToString(); }
            else if (conteneurId.HasValue) { subFolder = "Conteneurs"; entityIdFolder = conteneurId.Value.ToString(); }

            var targetDirectory = Path.Combine(_storageRootPath, subFolder, entityIdFolder);
            if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);

            var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
            var fullPath = Path.Combine(targetDirectory, uniqueFileName);

            if (fileStream.CanSeek) fileStream.Position = 0;
            using (var fileOutput = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileOutput);
            }

            var document = new Document
            {
                Nom = fileName,
                NomFichierOriginal = fileName,
                CheminFichier = Path.Combine(subFolder, entityIdFolder, uniqueFileName),
                Type = typeDoc,
                TypeMime = contentType,
                Extension = Path.GetExtension(fileName).ToLower(),
                TailleFichier = new FileInfo(fullPath).Length,
                DateCreation = DateTime.UtcNow,
                EstConfidentiel = estConfidentiel,
                ClientId = clientId,
                ColisId = colisId,
                VehiculeId = vehiculeId,
                ConteneurId = conteneurId,
                Actif = true
            };

            context.Documents.Add(document);
            await context.SaveChangesAsync();

            // --- AJOUT TIMELINE ---
            string desc = $"Document ajouté : {fileName} ({typeDoc})";
            await _timelineService.AddEventAsync(desc, colisId: colisId, vehiculeId: vehiculeId, conteneurId: conteneurId);
            // ----------------------

            // Déterminer le propriétaire (Client) pour le notifier
            Guid? ownerClientId = clientId;
            if (!ownerClientId.HasValue && colisId.HasValue) {
                var c = await context.Colis.FindAsync(colisId);
                ownerClientId = c?.ClientId;
            }
            // (Pareil pour Vehicule...)

            // URL de redirection vers l'entité parente
            string actionUrl = "";
            if (colisId.HasValue) actionUrl = $"/colis/edit/{colisId}";
            else if (vehiculeId.HasValue) actionUrl = $"/vehicule/edit/{vehiculeId}";
            else if (conteneurId.HasValue) actionUrl = $"/conteneur/detail/{conteneurId}";

            // Notifier Admin
            await _notificationService.CreateAndSendAsync(
                "Nouveau Document",
                $"Document ajouté : {fileName}",
                null, // Admin
                CategorieNotification.Document,
                actionUrl: actionUrl
            );

            // Notifier Client (si le doc n'est pas confidentiel)
            if (!estConfidentiel && ownerClientId.HasValue)
            {
                var clientUser = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == ownerClientId);
                if (clientUser != null)
                {
                    await _notificationService.CreateAndSendAsync(
                        "Nouveau Document",
                        $"Un document a été ajouté à votre dossier : {fileName}",
                        clientUser.Id,
                        CategorieNotification.Document,
                        actionUrl: actionUrl
                    );
                }
            }

            return document;
        }

        public async Task<Document?> GetByIdAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Document>> GetDocumentsByEntityAsync(Guid entityId, string entityType)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Documents.AsNoTracking().Where(d => d.Actif);

            // Filtrage dynamique selon le type
            switch (entityType.ToLower())
            {
                case "client":
                    query = query.Where(d => d.ClientId == entityId);
                    break;
                case "colis":
                    query = query.Where(d => d.ColisId == entityId);
                    break;
                case "vehicule":
                    query = query.Where(d => d.VehiculeId == entityId);
                    break;
                case "conteneur":
                    query = query.Where(d => d.ConteneurId == entityId);
                    break;
            }

            return await query.OrderByDescending(d => d.DateCreation).ToListAsync();
        }

        public async Task<(Stream FileStream, string ContentType, string FileName)?> GetFileStreamAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var doc = await context.Documents.FindAsync(id);

            if (doc == null) return null;

            var fullPath = Path.Combine(_storageRootPath, doc.CheminFichier);

            if (!File.Exists(fullPath))
            {
                // Gestion d'erreur si le fichier physique a disparu
                return null;
            }

            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return (stream, doc.TypeMime ?? "application/octet-stream", doc.NomFichierOriginal);
        }

        public async Task<bool> DeleteDocumentAsync(Guid id)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var doc = await context.Documents.FindAsync(id);

            if (doc == null) return false;

            // 1. Suppression Physique
            try
            {
                var fullPath = Path.Combine(_storageRootPath, doc.CheminFichier);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch
            {
                // On loggue l'erreur mais on continue la suppression logique
                // Idéalement injecter ILogger ici
            }

            // 2. Suppression Logique (ou physique en base si on préfère)
            // Ici on fait une suppression physique de la ligne BDD car le fichier n'existe plus
            context.Documents.Remove(doc);
            await context.SaveChangesAsync();

            // Logique de notification pour la suppression
            // Déterminer le propriétaire (Client) pour le notifier
            Guid? ownerClientId = doc.ClientId;
            if (!ownerClientId.HasValue && doc.ColisId.HasValue) {
                var c = await context.Colis.FindAsync(doc.ColisId);
                ownerClientId = c?.ClientId;
            }
            // (Pareil pour Vehicule...)

            // URL de redirection vers l'entité parente
            string actionUrl = "";
            if (doc.ColisId.HasValue) actionUrl = $"/colis/edit/{doc.ColisId}";
            else if (doc.VehiculeId.HasValue) actionUrl = $"/vehicule/edit/{doc.VehiculeId}";
            else if (doc.ConteneurId.HasValue) actionUrl = $"/conteneur/detail/{doc.ConteneurId}";

            // Notifier Admin
            await _notificationService.CreateAndSendAsync(
                "Document Supprimé",
                $"Document supprimé : {doc.Nom}",
                null, // Admin
                CategorieNotification.Document,
                actionUrl: actionUrl
            );

            // Notifier Client (si le doc n'est pas confidentiel)
            if (!doc.EstConfidentiel && ownerClientId.HasValue)
            {
                var clientUser = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == ownerClientId);
                if (clientUser != null)
                {
                    await _notificationService.CreateAndSendAsync(
                        "Document Supprimé",
                        $"Un document a été supprimé de votre dossier : {doc.Nom}",
                        clientUser.Id,
                        CategorieNotification.Document,
                        actionUrl: actionUrl
                    );
                }
            }

            return true;
        }

        public async Task<Document> RequestDocumentAsync(Guid entityId, TypeDocument type, Guid clientId, Guid? colisId = null, Guid? vehiculeId = null, string? commentaire = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var doc = new Document
            {
                Type = type,
                Statut = StatutDocument.Manquant,
                Nom = $"Document requis : {type}",
                ClientId = clientId,
                ColisId = colisId,
                VehiculeId = vehiculeId,
                CommentaireAdmin = commentaire,
                Actif = true,
                DateCreation = DateTime.UtcNow,
                // On met des valeurs par défaut pour les champs requis par EF mais vides pour un doc manquant
                CheminFichier = "PENDING", 
                NomFichierOriginal = "PENDING",
                Extension = ""
            };

            context.Documents.Add(doc);
            await context.SaveChangesAsync();

            // Notification au client
            var clientUser = await context.Utilisateurs.FirstOrDefaultAsync(u => u.ClientId == clientId);
            if (clientUser != null)
            {
                string actionUrl = "/"; // Par défaut Dashboard
                if (colisId.HasValue) actionUrl = $"/colis/detail/{colisId}"; // Vue détail côté client (supposé)
                else if (vehiculeId.HasValue) actionUrl = $"/vehicule/detail/{vehiculeId}";

                await _notificationService.CreateAndSendAsync(
                    "Document Manquant",
                    $"Un document ({type}) est requis pour votre dossier.",
                    clientUser.Id,
                    CategorieNotification.Document,
                    actionUrl: actionUrl,
                    priorite: PrioriteNotification.Haute,
                    relatedEntityId: doc.Id,
                    relatedEntityType: "Document"
                );
            }

            return doc;
        }

        public async Task<int> GetMissingDocumentsCountAsync(Guid clientId)
        {
             await using var context = await _contextFactory.CreateDbContextAsync();
             // On suppose que Document a été étendu avec ClientId (migré ou pas, sinon ça plantera à l'exec si pas migré, mais la migration a été appliquée)
             // La migration AddDocumentStatus n'a pas forcément ajouté ClientId, mais DocumentService l'utilise.
             // Vérifions Document.cs ... Si ClientId n'est pas dans Document, on ne peut pas filtrer facilement.
             // Mais si on a ajouté Statut, on peut filtrer 'Manquant'.
             // Supposons que la FK existe ou qu'on fait un join.
             // Pour l'instant on fait simple :
             return await context.Documents
                 .CountAsync(d => d.ClientId == clientId && d.Statut == StatutDocument.Manquant && d.Actif);
        }

        public async Task<Document?> GetFirstMissingDocumentAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Documents
                .Where(d => d.ClientId == clientId && d.Statut == StatutDocument.Manquant && d.Actif)
                .OrderByDescending(d => d.DateCreation) // ou Priority
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetPendingDocumentsCountAsync()
        {
             await using var context = await _contextFactory.CreateDbContextAsync();
             return await context.Documents
                .CountAsync(d => d.Statut == StatutDocument.EnAttenteValidation && d.Actif);
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(name.Where(ch => !invalidChars.Contains(ch)).ToArray()).Replace(" ", "_");
        }
    }
}
