using Microsoft.EntityFrameworkCore;
using TransitManager.Core.Entities;
using TransitManager.Infrastructure.Data;

namespace TransitManager.Infrastructure.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IDbContextFactory<TransitContext> _contextFactory;

        public SettingsService(IDbContextFactory<TransitContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<string> GetSettingAsync(string key, string defaultValue = "")
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var setting = await context.AppSettings.FindAsync(key);
            return setting?.Value ?? defaultValue;
        }

        public async Task UpdateSettingAsync(string key, string value)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var setting = await context.AppSettings.FindAsync(key);
            if (setting == null)
            {
                setting = new AppSetting { Key = key, Value = value };
                context.AppSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
            }
            await context.SaveChangesAsync();
        }
    }
}
