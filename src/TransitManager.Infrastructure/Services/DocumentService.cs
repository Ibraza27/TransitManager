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

        public DocumentService(IDbContextFactory<TransitContext> contextFactory, IConfiguration configuration)
        {
            _contextFactory = contextFactory;
            
            // On récupère le chemin de stockage depuis appsettings.json, sinon on utilise un défaut
            // "Storage" sera créé à la racine de l'exécution de l'API
            _storageRootPath = configuration["FileStorage:RootPath"] ?? Path.Combine(AppContext.BaseDirectory, "Storage");
            
            // Création du dossier racine s'il n'existe pas
            if (!Directory.Exists(_storageRootPath))
            {
                Directory.CreateDirectory(_storageRootPath);
            }
        }

        public async Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, TypeDocument typeDoc, Guid? clientId = null, Guid? colisId = null, Guid? vehiculeId = null, Guid? conteneurId = null, bool estConfidentiel = false)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // 1. Déterminer le sous-dossier (Organisation propre)
            string subFolder = "Misc";
            string entityIdFolder = Guid.NewGuid().ToString();

            if (clientId.HasValue) { subFolder = "Clients"; entityIdFolder = clientId.Value.ToString(); }
            else if (vehiculeId.HasValue) { subFolder = "Vehicules"; entityIdFolder = vehiculeId.Value.ToString(); }
            else if (colisId.HasValue) { subFolder = "Colis"; entityIdFolder = colisId.Value.ToString(); }
            else if (conteneurId.HasValue) { subFolder = "Conteneurs"; entityIdFolder = conteneurId.Value.ToString(); }

            var targetDirectory = Path.Combine(_storageRootPath, subFolder, entityIdFolder);
            if (!Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            // 2. Générer un nom de fichier unique pour éviter les écrasements
            // Format: [GUID]_[NomOriginal]
            var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
            var fullPath = Path.Combine(targetDirectory, uniqueFileName);

            // 3. Sauvegarder physiquement le fichier
            // On rembobine le stream si nécessaire
            if (fileStream.CanSeek) fileStream.Position = 0;
            
            using (var fileOutput = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileOutput);
            }

            // 4. Créer l'entrée en base de données
            var document = new Document
            {
                Nom = fileName, // Nom affiché
                NomFichierOriginal = fileName,
                CheminFichier = Path.Combine(subFolder, entityIdFolder, uniqueFileName), // Chemin relatif stocké
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

            return true;
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(name.Where(ch => !invalidChars.Contains(ch)).ToArray()).Replace(" ", "_");
        }
    }
}