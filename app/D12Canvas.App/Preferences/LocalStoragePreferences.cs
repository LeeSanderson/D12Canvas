using Microsoft.JSInterop;

namespace D12Canvas.App.Preferences;

public sealed class LocalStoragePreferences(IJSRuntime js) : IPreferences
{
    public async Task<string?> GetAsync(string key) =>
        await js.InvokeAsync<string?>("localStorage.getItem", key);

    public async Task SetAsync(string key, string value) =>
        await js.InvokeVoidAsync("localStorage.setItem", key, value);
}
