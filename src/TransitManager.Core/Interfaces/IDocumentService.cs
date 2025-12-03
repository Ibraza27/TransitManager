using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;

namespace TransitManager.Core.Interfaces
{
    public interface IDocumentService
    {
        // Upload générique
        Task<Document> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, TypeDocument typeDoc, Guid? clientId = null, Guid? colisId = null, Guid? vehiculeId = null, Guid? conteneurId = null, bool estConfidentiel = false);
        
        // Récupération
        Task<Document?> GetByIdAsync(Guid id);
        Task<IEnumerable<Document>> GetDocumentsByEntityAsync(Guid entityId, string entityType); // entityType = "Client", "Vehicule", etc.
        
        // Téléchargement (renvoie le stream du fichier et son content type)
        Task<(Stream FileStream, string ContentType, string FileName)?> GetFileStreamAsync(Guid id);
        
        // Suppression (Logique + Physique)
        Task<bool> DeleteDocumentAsync(Guid id);
    }
}