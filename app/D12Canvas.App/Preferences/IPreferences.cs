namespace D12Canvas.App.Preferences;

public interface IPreferences
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
}
