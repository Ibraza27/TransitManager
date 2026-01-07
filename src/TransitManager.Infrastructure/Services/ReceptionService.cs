using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransitManager.Core.Entities;
using TransitManager.Core.Interfaces;
using TransitManager.Core.DTOs;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public interface IReceptionService
    {
        Task<ReceptionControl> CreateControlAsync(ReceptionControl control);
        Task<ReceptionControl?> GetByEntityAsync(string entityType, Guid entityId);
        Task<List<ReceptionControl>> GetRecentControlsAsync(int count = 50);
        Task<ReceptionStatsDto> GetStatsAsync(DateTime? start = null, DateTime? end = null);
        Task<bool> DeleteControlAsync(Guid id);
    }



    public class ReceptionService : IReceptionService
    {
        private readonly TransitContext _context;
        private readonly INotificationService _notificationService;

        public ReceptionService(TransitContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ReceptionControl> CreateControlAsync(ReceptionControl control)
        {
            control.CreationDate = DateTime.UtcNow; // Ensure valid date
            _context.ReceptionControls.Add(control);
            await _context.SaveChangesAsync();

            // Notify Admin
            var client = await _context.Clients.FindAsync(control.ClientId); // Fetch for name
            await _notificationService.NotifySavSubmissionAsync(control, client?.NomComplet ?? "Client Inconnu");

            return control;
        }

        public async Task<ReceptionControl?> GetByEntityAsync(string entityType, Guid entityId)
        {
            if (entityType == "Colis")
            {
                return await _context.ReceptionControls
                    .Include(rc => rc.Issues)
                    .FirstOrDefaultAsync(rc => rc.ColisId == entityId);
            }
            else if (entityType == "Vehicule")
            {
                return await _context.ReceptionControls
                    .Include(rc => rc.Issues)
                    .FirstOrDefaultAsync(rc => rc.VehiculeId == entityId);
            }
            return null;
        }

        public async Task<List<ReceptionControl>> GetRecentControlsAsync(int count = 50)
        {
            return await _context.ReceptionControls
                .Include(c => c.Client)
                .Include(c => c.Issues)
                .OrderByDescending(c => c.CreationDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<ReceptionStatsDto> GetStatsAsync(DateTime? start = null, DateTime? end = null)
        {
            var stats = new ReceptionStatsDto();
            
            var query = _context.ReceptionControls.AsQueryable();

            if (start.HasValue)
                query = query.Where(c => c.CreationDate >= start.Value.ToUniversalTime());
            
            if (end.HasValue)
                query = query.Where(c => c.CreationDate <= end.Value.ToUniversalTime());

            var all = await query.ToListAsync();

            if (!all.Any()) return stats;

            stats.TotalControls = all.Count;
            stats.AverageService = all.Average(c => c.RateService);
            stats.AverageCondition = all.Average(c => c.RateCondition);
            stats.AverageCommunication = all.Average(c => c.RateCommunication);
            stats.AverageRecommendation = all.Average(c => c.RateRecommendation);

            stats.CountFull = all.Count(c => c.Status == ReceptionStatus.ReceivedFull);
            stats.CountPartial = all.Count(c => c.Status == ReceptionStatus.ReceivedPartial);
            stats.CountDamaged = all.Count(c => c.Status == ReceptionStatus.ReceivedDamaged);
            
            stats.ControlsWithIssues = stats.CountPartial + stats.CountDamaged;
            stats.IssuePercentage = stats.TotalControls > 0 ? (double)stats.ControlsWithIssues / stats.TotalControls * 100 : 0;
            
            // Global Average
            if (stats.TotalControls > 0)
            {
                stats.AverageRating = (stats.AverageService + stats.AverageCondition + stats.AverageCommunication + stats.AverageRecommendation) / 4;
            }
            
            // Monthly Trend (Based on the filtered period, or last 6 months if no filter)
            // If filter is small (e.g. 1 month), we might want daily? 
            // For now keep monthly logic but respect the filter.
            
            // However, the user asked for "stats of this period". Trends usually show evolution.
            // If a period is selected, the "Trends" chart might look flat if it's just one month.
            // Let's adapt: if start is set, use it. Else default to 6 months ago.
            var trendStart = start.HasValue ? start.Value : DateTime.UtcNow.AddMonths(-5).Date;
            
            stats.MonthlyCount = all
                .Where(c => c.CreationDate >= trendStart)
                .GroupBy(c => c.CreationDate.ToString("MMM yyyy"))
                .ToDictionary(g => g.Key, g => g.Count());

            return stats;
        }

        public async Task<bool> DeleteControlAsync(Guid id)
        {
            var control = await _context.ReceptionControls
                .Include(c => c.Issues)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (control == null) return false;

            // Explicitly remove issues to avoid FK constraint errors if cascade is not set
            if (control.Issues.Any())
            {
                _context.ReceptionIssues.RemoveRange(control.Issues);
            }

            _context.ReceptionControls.Remove(control);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
