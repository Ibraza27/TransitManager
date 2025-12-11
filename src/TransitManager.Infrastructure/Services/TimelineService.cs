using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TransitManager.Core.DTOs;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using TransitManager.Infrastructure.Hubs;

namespace TransitManager.Infrastructure.Services
{
    public class TimelineService : ITimelineService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;
		private readonly IHubContext<AppHub> _hubContext;

		public TimelineService(IDbContextFactory<TransitContext> contextFactory, IHubContext<AppHub> hubContext)
		{
			_contextFactory = contextFactory;
			_hubContext = hubContext;
		}
        public async Task AddEventAsync(string description, Guid? colisId = null, Guid? vehiculeId = null, Guid? conteneurId = null, string? statut = null, string? location = null)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var evt = new TrackingEvent
            {
                Description = description,
                ColisId = colisId,
                VehiculeId = vehiculeId,
                ConteneurId = conteneurId,
                Statut = statut,
                Location = location,
                EventDate = DateTime.UtcNow,
                IsAutomatic = true
            };

            context.TrackingEvents.Add(evt);
            await context.SaveChangesAsync();
			
			// --- TEMPS RÉEL ---
			var dto = new TimelineDto
			{
				Date = evt.EventDate,
				Description = evt.Description,
				Location = evt.Location,
				Statut = evt.Statut,
				// Recopier la logique DetermineIcon/Color ici ou rendre ces méthodes statiques/publiques
				IconKey = "info-circle", 
				ColorHex = "#808080"
			};

			if (colisId.HasValue) 
				await _hubContext.Clients.Group(colisId.ToString()).SendAsync("ReceiveTimelineEvent", dto);
			
			if (vehiculeId.HasValue) 
				await _hubContext.Clients.Group(vehiculeId.ToString()).SendAsync("ReceiveTimelineEvent", dto);
			
        }

        public async Task<IEnumerable<TimelineDto>> GetTimelineAsync(Guid? colisId, Guid? vehiculeId)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var query = context.TrackingEvents.AsNoTracking();

            if (colisId.HasValue)
                query = query.Where(t => t.ColisId == colisId);
            else if (vehiculeId.HasValue)
                query = query.Where(t => t.VehiculeId == vehiculeId);
            else
                return new List<TimelineDto>();

            var events = await query.OrderByDescending(t => t.EventDate).ToListAsync();

            return events.Select(e => new TimelineDto
            {
                Date = e.EventDate,
                Description = e.Description,
                Location = e.Location,
                Statut = e.Statut,
                // Logique simple pour l'icône (à affiner plus tard)
                IconKey = DetermineIcon(e.Description, e.Statut),
                ColorHex = DetermineColor(e.Statut)
            });
        }

        private string DetermineIcon(string desc, string? statut)
        {
            if (desc.Contains("Création")) return "plus-circle";
            if (desc.Contains("Conteneur")) return "box-seam";
            if (desc.Contains("Paiement")) return "cash";
            if (statut == "Livre") return "check-circle";
            return "clock";
        }

        private string DetermineColor(string? statut)
        {
            if (statut == "Livre") return "#28a745"; // Vert
            if (statut == "Probleme") return "#dc3545"; // Rouge
            return "#0d6efd"; // Bleu par défaut
        }
    }
}