using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace D12Canvas;

// Ticket 62/ADR 0009: a pure presentation component - it knows nothing about Board/Selection, only
// the anchor point, Group/Ungroup eligibility (computed by DiagramCanvas), and the action callbacks
// to invoke. DiagramCanvas wires every callback directly to the same OnXPressed method its own
// keyboard shortcuts call, so a menu action is always the identical undoable command.
public partial class SelectionContextMenu : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Parameter]
    public double X { get; set; }

    [Parameter]
    public double Y { get; set; }

    [Parameter]
    public bool CanGroup { get; set; }

    [Parameter]
    public bool CanUngroup { get; set; }

    [Parameter]
    public EventCallback OnDelete { get; set; }

    [Parameter]
    public EventCallback OnGroup { get; set; }

    [Parameter]
    public EventCallback OnUngroup { get; set; }

    [Parameter]
    public EventCallback OnBringToFront { get; set; }

    [Parameter]
    public EventCallback OnSendToBack { get; set; }

    [Parameter]
    public EventCallback OnBringForward { get; set; }

    [Parameter]
    public EventCallback OnSendBackward { get; set; }

    // Fired on Escape (handled locally, see HandleKeyDown) or a mousedown outside the menu (see
    // OnClickOutside) - either way DiagramCanvas is the one that owns whether a menu is open at all.
    [Parameter]
    public EventCallback OnRequestClose { get; set; }

    private ElementReference _menuRef;
    private DotNetObjectReference<SelectionContextMenu>? _dotNetRef;
    private IJSObjectReference? _jsModule;

    private string MenuStyle => $"left: {X}px; top: {Y}px;";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/D12Canvas/SelectionContextMenu.razor.js"
            );
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync("registerClickOutside", _menuRef, _dotNetRef);
        }
    }

    [JSInvokable]
    public void OnClickOutside() => OnRequestClose.InvokeAsync();

    // ADR 0009's addendum precedent (also used by Text/StickyNote's inline editor): stopping
    // propagation here means this Escape never reaches DiagramCanvas's own window-level keydown
    // listener, so it closes only this menu rather than also clearing the board selection.
    // ArrowDown/ArrowUp roving focus fulfils the role="menu"/"menuitem" contract declared in the
    // markup - those roles cue assistive tech to expect arrow-key navigation, not just Tab order.
    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Escape":
                await OnRequestClose.InvokeAsync();
                break;
            case "ArrowDown":
                await _jsModule!.InvokeVoidAsync("focusAdjacentItem", _menuRef, 1);
                break;
            case "ArrowUp":
                await _jsModule!.InvokeVoidAsync("focusAdjacentItem", _menuRef, -1);
                break;
        }
    }

    private Task InvokeDelete() => OnDelete.InvokeAsync();

    private Task InvokeGroup() => OnGroup.InvokeAsync();

    private Task InvokeUngroup() => OnUngroup.InvokeAsync();

    private Task InvokeBringToFront() => OnBringToFront.InvokeAsync();

    private Task InvokeSendToBack() => OnSendToBack.InvokeAsync();

    private Task InvokeBringForward() => OnBringForward.InvokeAsync();

    private Task InvokeSendBackward() => OnSendBackward.InvokeAsync();

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            await _jsModule.InvokeVoidAsync("unregisterClickOutside");
            await _jsModule.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }
}
