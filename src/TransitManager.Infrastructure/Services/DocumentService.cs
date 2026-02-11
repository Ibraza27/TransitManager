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
using TransitManager.Core.DTOs;
using TransitManager.Core.DTOs.Settings;

namespace TransitManager.Infrastructure.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
        private readonly string _storageRootPath;
        private readonly ITimelineService _timelineService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ISettingsService _settingsService;

        public DocumentService(
            IDbContextFactory<TransitContext> contextFactory,
            IConfiguration configuration,
            ITimelineService timelineService,
            INotificationService notificationService,
            IEmailService emailService,
            ISettingsService settingsService)
        {
            _contextFactory = contextFactory;
            _timelineService = timelineService;
            _notificationService = notificationService;
            _emailService = emailService;
            _settingsService = settingsService;

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

            // --- SÉCURITÉ : Validation du type de fichier (Fix V3) ---
            var allowedExtensions = new Dictionary<string, List<byte[]>>
            {
                {".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } },
                { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
                { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
                { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47 } } },
                { ".docx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
                { ".xlsx", new List<byte[]> { new byte[] { 0x50, 0x4B, 0x03, 0x04 } } },
                // Vidéo / Audio
                { ".mov", new List<byte[]> { 
                    new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70 }, // ftypqt
                    new byte[] { 0x6D, 0x6F, 0x6F, 0x76 }, // moov (si pas au début, risque d'échec check magic bytes simple - à affiner si besoin)
                    new byte[] { 0x66, 0x72, 0x65, 0x65 } // free
                } }, 
                { ".mp4", new List<byte[]> { 
                    new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 },
                    new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 },
                    new byte[] { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70 },
                    new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70 } // mp42/isom variants
                } },
                { ".mp3", new List<byte[]> { 
                    new byte[] { 0x49, 0x44, 0x33 }, // ID3
                    new byte[] { 0xFF, 0xFB }, // MPEG-1 Layer 3 (approx)
                    new byte[] { 0xFF, 0xF3 }, // MPEG-1 Layer 3 (approx)
                    new byte[] { 0xFF, 0xF2 } 
                } },
                { ".avi", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }, // RIFF
                { ".wav", new List<byte[]> { new byte[] { 0x52, 0x49, 0x46, 0x46 } } }, // RIFF
                { ".mkv", new List<byte[]> { new byte[] { 0x1A, 0x45, 0xDF, 0xA3 } } }, // Matroska
                { ".m4a", new List<byte[]> { new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x4D, 0x34, 0x41 } } } // M4A start
            };

            var ext = Path.GetExtension(fileName).ToLower();
            if (!allowedExtensions.ContainsKey(ext))
            {
                throw new InvalidOperationException($"Type de fichier non autorisé : {ext}");
            }

            // Vérification Magic Bytes
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
                // Lecture de 32 octets pour avoir assez de contexte (notamment pour ftyp)
                var buffer = new byte[32]; 
                await fileStream.ReadAsync(buffer, 0, 32);
                fileStream.Position = 0; // Reset pour copie

                bool isMatch = false;

                // Logique spécifique pour MP4, MOV, M4A (Conteneurs ISO Base Media)
                // Structure typique : [4 bytes size] [4 bytes 'ftyp'] ...
                if (ext == ".mp4" || ext == ".mov" || ext == ".m4a")
                {
                    // Vérifier si 'ftyp' (0x66 0x74 0x79 0x70) est présent à l'offset 4
                    var ftyp = new byte[] { 0x66, 0x74, 0x79, 0x70 };
                    if (buffer.Skip(4).Take(4).SequenceEqual(ftyp))
                    {
                        isMatch = true;
                    }
                    // Fallback : vérifier 'moov' (0x6D 0x6F 0x6F 0x76) au début ou à l'offset 4 (moins standard mais possible)
                    else if (buffer.Take(4).SequenceEqual(new byte[] { 0x6D, 0x6F, 0x6F, 0x76 })) isMatch = true; 
                }
                
                // Si pas matché par logique spécifique, on utilise les signatures standard
                if (!isMatch && allowedExtensions.ContainsKey(ext))
                {
                    var signatures = allowedExtensions[ext];
                    // On vérifie si LE DÉBUT du buffer correspond à une signature
                    isMatch = signatures.Any(sig => 
                        buffer.Length >= sig.Length && buffer.Take(sig.Length).SequenceEqual(sig));
                }

                if (!isMatch)
                {
                    // Pour le debug, on peut logger les bytes (optionnel, supprimer en prod)
                    var hex = BitConverter.ToString(buffer.Take(8).ToArray());
                    Console.WriteLine($"[MagicBytes] Echec pour {fileName} ({ext}). Header: {hex}");
                    
                    throw new InvalidOperationException($"Le contenu du fichier ne correspond pas à son extension ({ext}). (Header: {hex})");
                }
            }
            // ---------------------------------------------------------

            var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
            var fullPath = Path.Combine(targetDirectory, uniqueFileName);

            using (var fileOutput = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileOutput);
            }

            // Déterminer le propriétaire (Client) si non fourni
            if (!clientId.HasValue)
            {
                if (colisId.HasValue) 
                {
                    var c = await context.Colis.FindAsync(colisId);
                    clientId = c?.ClientId;
                }
                else if (vehiculeId.HasValue) 
                {
                    var v = await context.Vehicules.FindAsync(vehiculeId);
                    clientId = v?.ClientId;
                }
                // Pour conteneur, pas forcément de Client unique, donc on laisse null ou on gère autrement
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
                ClientId = clientId, // Utilisation du ClientId déduit ou fourni
                ColisId = colisId,
                VehiculeId = vehiculeId,
                ConteneurId = conteneurId,
 
                // CORRECTION : On met EnAttenteValidation par défaut pour qu'il apparaisse dans le Dashboard
                Statut = StatutDocument.EnAttenteValidation,
                Actif = true
            };

            context.Documents.Add(document);
            await context.SaveChangesAsync();

            // --- AJOUT TIMELINE ---
            string desc = $"Document ajouté : {fileName} ({typeDoc})";
            await _timelineService.AddEventAsync(desc, colisId: colisId, vehiculeId: vehiculeId, conteneurId: conteneurId);
            // ----------------------

            // URL de redirection vers l'entité parente
            string actionUrl = "";
            
            // LOGIQUE DEEP LINKING DOCUMENTS FINANCIERS
            var financialTypes = new[] { TypeDocument.Facture, TypeDocument.Recu, TypeDocument.Devis, TypeDocument.Contrat };
            if (financialTypes.Contains(typeDoc))
            {
                 actionUrl = "/finance?tab=documents";
            }
            else
            {
                if (colisId.HasValue) actionUrl = $"/colis/edit/{colisId}?tab=documents";
                else if (vehiculeId.HasValue) actionUrl = $"/vehicule/edit/{vehiculeId}?tab=documents";
                else if (conteneurId.HasValue) actionUrl = $"/conteneur/detail/{conteneurId}?tab=documents";
            }

            // Notifier Admin
            await _notificationService.CreateAndSendAsync(
                "Nouveau Document",
                $"Document ajouté : {fileName}",
                null, // Admin
                CategorieNotification.Document,
                actionUrl: actionUrl
            );

            var ownerClientId = clientId;

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
            Console.WriteLine($"[DEBUG] GetFileStreamAsync: ID={id}, Path={fullPath}, Exists={File.Exists(fullPath)}");

            if (!File.Exists(fullPath))
            {
                // Gestion d'erreur si le fichier physique a disparu
                Console.WriteLine($"[ERROR] File not found at {fullPath}");
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
            catch (Exception ex)
            {
                // Idéalement injecter ILogger ici
                Console.WriteLine($"[DocumentService] Error deleting file: {ex.Message}");
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

            // CLEANUP : On supprime la notification "Nouveau Document" qui était liée à ce fichier
            await _notificationService.DeleteByEntityAsync(id, CategorieNotification.Document);

            // URL de redirection vers l'entité parente
            string actionUrl = "";
            if (doc.ColisId.HasValue) actionUrl = $"/colis/edit/{doc.ColisId}?tab=docs";
            else if (doc.VehiculeId.HasValue) actionUrl = $"/vehicule/edit/{doc.VehiculeId}?tab=docs";
            else if (doc.ConteneurId.HasValue) actionUrl = $"/conteneur/detail/{doc.ConteneurId}?tab=docs";

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

        public async Task<Document?> UpdateDocumentAsync(Guid id, UpdateDocumentDto dto)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var doc = await context.Documents.FindAsync(id);

            if (doc == null) return null;

            // 1. Mise à jour du Type
            var oldType = doc.Type;
            doc.Type = dto.Type;

            // 2. LOGIQUE MÉTIER : Si on change le type pour un type qui était "Manquant", on supprime l'alerte
            if (doc.Type != oldType)
            {
                // Chercher si un document "Manquant" de ce nouveau type existe pour la même entité
                var missingDocs = context.Documents.Where(d => 
                    d.Statut == StatutDocument.Manquant && 
                    d.Type == doc.Type &&
                    d.Actif &&
                    (d.ClientId == doc.ClientId || (d.ColisId != null && d.ColisId == doc.ColisId) || (d.VehiculeId != null && d.VehiculeId == doc.VehiculeId))
                );
                
                context.Documents.RemoveRange(missingDocs);
            }

            // 3. Mise à jour du Nom (Renommage Physique)
            // L'utilisateur envoie le nom SANS extension (ex: "Mon Fichier"). On doit rajouter l'extension.
            if (!string.IsNullOrWhiteSpace(dto.Nom))
            {
                // Si l'utilisateur n'a pas mis l'extension, on la rajoute pour stocker un nom complet correct
                string newNameWithExt = dto.Nom;
                if (!newNameWithExt.EndsWith(doc.Extension, StringComparison.OrdinalIgnoreCase))
                {
                    newNameWithExt += doc.Extension;
                }

                if (!newNameWithExt.Equals(doc.Nom, StringComparison.OrdinalIgnoreCase))
                {
                    var oldPath = Path.Combine(_storageRootPath, doc.CheminFichier);
                    var directory = Path.GetDirectoryName(oldPath); // Use full path to verify existence? No, use oldPath logic
                    // But wait, the previous fix used Path.GetDirectoryName(doc.CheminFichier) !! 
                    // Let's stick to what works.
                    
                    // On recalcule le repertoire parent relatif de la même manière que le fix précédent
                    var relativeDir = Path.GetDirectoryName(doc.CheminFichier) ?? "";
                    var fullOldPath = Path.Combine(_storageRootPath, doc.CheminFichier);

                    if (File.Exists(fullOldPath))
                    {
                         // Use just the NAME part for the sanitized filename to avoid double extension if we want.
                         // Standard logic: GUID_SanitizedName.ext
                         // dto.Nom might be "File" or "File.pdf".
                         // SanitizeFileName("File") -> "File". Result: GUID_File.pdf
                         // SanitizeFileName("File.pdf") -> "File_pdf". Result: GUID_File_pdf.pdf (Ugly)
                         
                         // We prefer to sanitize just the name-without-extension part.
                         string namePart = Path.GetFileNameWithoutExtension(newNameWithExt);
                         var sanitizedNewName = SanitizeFileName(namePart);
                         
                         var newFileName = $"{Guid.NewGuid()}_{sanitizedNewName}{doc.Extension}";
                         var newPath = Path.Combine(_storageRootPath, relativeDir, newFileName);
                         
                         try
                         {
                             // Ensure directory exists (it should)
                             var targetDir = Path.GetDirectoryName(newPath);
                             if (targetDir != null && !Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                             File.Move(fullOldPath, newPath);
                             
                             // Update DB
                             doc.CheminFichier = Path.Combine(relativeDir, newFileName);
                         }
                         catch (Exception ex)
                         {
                             // Log and abort rename, but maybe allow other updates? No, rename is critical.
                             Console.WriteLine($"[ERROR] Rename failed: {ex.Message}");
                             throw new IOException($"Erreur lors du renommage physique: {ex.Message}", ex);
                         }
                    }
                }
                
                doc.Nom = newNameWithExt;
            }

            await context.SaveChangesAsync();
            return doc;
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
                if (colisId.HasValue) actionUrl = $"/colis/detail/{colisId}?tab=docs"; // Vue détail côté client (supposé)
                else if (vehiculeId.HasValue) actionUrl = $"/vehicule/detail/{vehiculeId}?tab=docs";

                // 1. Notification Interne (SignalR)
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

                // 2. Notification Email (Nouvelle Fonctionnalité)
                try
                {
                    if (!string.IsNullOrEmpty(clientUser.Email))
                    {
                        var companyProfile = await _settingsService.GetSettingAsync<CompanyProfileDto>("CompanyProfile", new());
                        var clientName = $"{clientUser.Prenom} {clientUser.Nom}".Trim();
                        // Lien direct vers le portail (base URL hardcodée ou via config - ici on improvise un peu pour le portail client)
                        // Idéalement récupérer BaseUrl via IConfiguration ou Settings.
                        // On assume "https://hippocampetransitmanager.com" pour la prod.
                        var portalBaseUrl = "https://hippocampetransitmanager.com"; 
                        var fullLink = $"{portalBaseUrl}{actionUrl}";
                        
                        // Fallback Logo URL si vide dans le profil
                        var logoUrl = !string.IsNullOrEmpty(companyProfile.LogoUrl) 
                            ? (companyProfile.LogoUrl.StartsWith("http") ? companyProfile.LogoUrl : $"{portalBaseUrl}/{companyProfile.LogoUrl}")
                            : "https://hippocampetransitmanager.com/images/logo.jpg";
                            
                         // Ensure logo ends with .jpg if it was .png (fix legacy config)
                        if(logoUrl.EndsWith(".png")) logoUrl = logoUrl.Replace(".png", ".jpg");

                        await _emailService.SendMissingDocumentNotificationAsync(
                            clientUser.Email,
                            clientName,
                            type.ToString(),
                            commentaire,
                            fullLink,
                            companyProfile.CompanyName,
                            $"{companyProfile.Address}, {companyProfile.ZipCode} {companyProfile.City}",
                            companyProfile.Phone,
                            logoUrl
                        );
                    }
                }
                catch (Exception ex)
                {
                    // On ne veut pas faire échouer la demande si l'email plante
                    Console.WriteLine($"[DocumentService] Erreur envoi email: {ex.Message}");
                }
            }

            return doc;
        }

        public async Task<int> GetMissingDocumentsCountAsync(Guid clientId)
        {
             await using var context = await _contextFactory.CreateDbContextAsync();
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

        public async Task<IEnumerable<Document>> GetMissingDocumentsAsync(Guid clientId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Documents
                .Where(d => d.ClientId == clientId && d.Statut == StatutDocument.Manquant && d.Actif)
                .OrderByDescending(d => d.DateCreation)
                .ToListAsync();
        }

        public async Task<int> GetPendingDocumentsCountAsync()
        {
             await using var context = await _contextFactory.CreateDbContextAsync();
             return await context.Documents
                .CountAsync(d => d.Statut == StatutDocument.EnAttenteValidation && d.Actif);
        }

        public async Task<int> GetTotalMissingDocumentsCountAsync()
        {
             await using var context = await _contextFactory.CreateDbContextAsync();
             return await context.Documents
                 .CountAsync(d => d.Statut == StatutDocument.Manquant && d.Actif);
        }

        public async Task<IEnumerable<Document>> GetAllMissingDocumentsAsync()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Documents
                .Include(d => d.Client)
                .Include(d => d.Vehicule)
                .Include(d => d.Colis)
                .Where(d => d.Statut == StatutDocument.Manquant && d.Actif)
                .OrderByDescending(d => d.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Document>> GetFinancialDocumentsAsync(int? year, int? month, TypeDocument? type, Guid? clientId = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var query = context.Documents.Include(d => d.Client).AsQueryable();

            if (clientId.HasValue)
                query = query.Where(d => d.ClientId == clientId.Value);

            if (year.HasValue)
                query = query.Where(d => d.DateCreation.Year == year.Value);

            if (month.HasValue)
                query = query.Where(d => d.DateCreation.Month == month.Value);

            if (type.HasValue)
            {
                query = query.Where(d => d.Type == type.Value);
            }
            else
            {
                var financialTypes = new[] 
                { 
                    TypeDocument.Facture, 
                    TypeDocument.Recu, 
                    TypeDocument.Devis, 
                    TypeDocument.Contrat,
                    TypeDocument.BordereauExpedition
                };
                query = query.Where(d => financialTypes.Contains(d.Type));
            }

            return await query.OrderByDescending(d => d.DateCreation).ToListAsync();
        }

        // --- Gestion des Fichiers Temporaires ---

        public async Task<Guid> UploadTempDocumentAsync(Stream fileStream, string fileName)
        {
            var tempDir = Path.Combine(_storageRootPath, "Temp");
            if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

            var tempId = Guid.NewGuid();
            var sanitizedName = SanitizeFileName(fileName);
            var safeName = $"{tempId}_{sanitizedName}";
            var fullPath = Path.Combine(tempDir, safeName);

            using (var fileOutput = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileOutput);
            }

            return tempId;
        }

        public async Task<(Stream FileStream, string FileName)?> GetTempDocumentAsync(Guid id)
        {
             var tempDir = Path.Combine(_storageRootPath, "Temp");
             if (!Directory.Exists(tempDir)) return null;

             // Find file starting with id
             var file = Directory.GetFiles(tempDir, $"{id}_*").FirstOrDefault();
             if (file == null) return null;

             var fileName = Path.GetFileName(file);
             // Remove ID prefix for original name restoration (approximate)
             // Format: {id}_{name}
             var originalName = fileName.Substring(id.ToString().Length + 1); // +1 underscore

             var stream = new FileStream(file, FileMode.Open, FileAccess.Read);
             return (stream, originalName);
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(name.Where(ch => !invalidChars.Contains(ch)).ToArray()).Replace(" ", "_");
        }
    }
}
