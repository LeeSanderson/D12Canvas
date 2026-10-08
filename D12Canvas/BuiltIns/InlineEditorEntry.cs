using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace D12Canvas.BuiltIns;

internal static class InlineEditorEntry
{
    public const string ModulePath = "./_content/D12Canvas/inlineEditor.js";

    private static readonly ConditionalWeakTable<IJSRuntime, Task<IJSObjectReference>> Modules =
        new();

    public static async Task FocusAndSelectAllAsync(IJSRuntime js, ElementReference editor)
    {
        try
        {
            var module = await Modules.GetValue(
                js,
                runtime => runtime.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask()
            );
            await module.InvokeVoidAsync("focusAndSelectAll", editor);
        }
        catch (JSDisconnectedException) { }
    }
}
