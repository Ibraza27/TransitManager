using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;

namespace TransitManager.Core.Interfaces
{
    public interface ITimelineService
    {
        // Ajouter un événement manuellement ou automatiquement
        Task AddEventAsync(string description, Guid? colisId = null, Guid? vehiculeId = null, Guid? conteneurId = null, string? statut = null, string? location = null);
        
        // Récupérer l'historique complet pour l'affichage
        Task<IEnumerable<TimelineDto>> GetTimelineAsync(Guid? colisId, Guid? vehiculeId);
    }
}