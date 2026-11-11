using Microsoft.JSInterop;

namespace D12Canvas.App.Components;

public sealed class LazyJsModule(IJSRuntime js, string path) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async ValueTask<IJSObjectReference> GetAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", path);

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
    }
}
