using Bunit;
using D12Canvas.App.Preferences;

namespace D12Canvas.App.Tests;

public sealed class LocalStoragePreferencesTests : BunitContext
{
    private readonly LocalStoragePreferences _preferences;

    public LocalStoragePreferencesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        _preferences = new LocalStoragePreferences(JSInterop.JSRuntime);
    }

    [Fact]
    public async Task GetReadsTheKeyFromLocalStorage()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "d12canvas.theme").SetResult("dark");

        var value = await _preferences.GetAsync("d12canvas.theme");

        Assert.Equal("dark", value);
    }

    [Fact]
    public async Task GetReturnsNullForAMissingKey()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "d12canvas.autosave").SetResult(null);

        var value = await _preferences.GetAsync("d12canvas.autosave");

        Assert.Null(value);
    }

    [Fact]
    public async Task SetWritesTheKeyToLocalStorage()
    {
        var write = JSInterop
            .SetupVoid("localStorage.setItem", "d12canvas.autosave", "false")
            .SetVoidResult();

        await _preferences.SetAsync("d12canvas.autosave", "false");

        Assert.Single(write.Invocations);
    }
}
