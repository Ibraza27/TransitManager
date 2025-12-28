namespace TransitManager.Infrastructure.Services
{
    public interface ISettingsService
    {
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task UpdateSettingAsync(string key, string value);
    }
}
