using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TransitManager.Core.Entities;
using TransitManager.Core.Enums;
using TransitManager.Core.DTOs;

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

        // Mise à jour (Nom + Type + Renommage physique)
        Task<Document?> UpdateDocumentAsync(Guid id, UpdateDocumentDto dto);

        // Demande de document (Admin -> Client)
        Task<Document> RequestDocumentAsync(Guid entityId, TypeDocument type, Guid clientId, Guid? colisId = null, Guid? vehiculeId = null, string? commentaire = null);
        
        // Compteur Dashboard Client
        Task<int> GetMissingDocumentsCountAsync(Guid clientId);
        Task<IEnumerable<Document>> GetMissingDocumentsAsync(Guid clientId);
        
        // Helper pour redirection UI
        Task<Document?> GetFirstMissingDocumentAsync(Guid clientId);

        // Stats Admin
        Task<int> GetPendingDocumentsCountAsync();
        Task<int> GetTotalMissingDocumentsCountAsync();
        Task<IEnumerable<Document>> GetAllMissingDocumentsAsync();
    }
}