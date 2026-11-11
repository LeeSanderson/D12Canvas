using D12Canvas.App.Preferences;

namespace D12Canvas.App.Tests;

public sealed class InMemoryPreferences : IPreferences
{
    public Dictionary<string, string> Values { get; } = [];

    public Task<string?> GetAsync(string key) =>
        Task.FromResult(Values.TryGetValue(key, out var value) ? value : null);

    public Task SetAsync(string key, string value)
    {
        Values[key] = value;
        return Task.CompletedTask;
    }
}
