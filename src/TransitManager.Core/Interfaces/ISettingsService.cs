using System.Threading.Tasks;

namespace TransitManager.Core.Interfaces
{
    public interface ISettingsService
    {
        Task<string> GetSettingAsync(string key, string defaultValue = "");
        Task<T> GetSettingAsync<T>(string key, T defaultValue);
        Task UpdateSettingAsync(string key, string value);
        Task SaveSettingAsync<T>(string key, T value);
    }
}
