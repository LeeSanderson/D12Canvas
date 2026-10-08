using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace D12Canvas;

// One menu for both content sets. It is handed the context DiagramCanvas resolved when it opened,
// composes its rows from that, places itself inside the container and reports which command was
// chosen; DiagramCanvas runs the same method the command's chord runs, so a row is always the
// identical undoable command.
public partial class ContextMenu : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Parameter, EditorRequired]
    public ContextMenuContext Context { get; set; } = null!;

    [Parameter]
    public double X { get; set; }

    [Parameter]
    public double Y { get; set; }

    [Parameter]
    public double ContainerWidth { get; set; }

    [Parameter]
    public double ContainerHeight { get; set; }

    // A keyboard-opened menu focuses its first row, and on closing hands focus back to whatever held
    // it when the menu opened. A pointer-opened menu focuses itself and hands nothing back.
    [Parameter]
    public bool OpenedFromKeyboard { get; set; }

    [Parameter]
    public EventCallback<ContextMenuCommand> OnInvoke { get; set; }

    // Fired on Escape inside the menu or a press anywhere outside it; DiagramCanvas owns whether a
    // menu is open at all.
    [Parameter]
    public EventCallback OnRequestClose { get; set; }

    private ElementReference _menuRef;
    private DotNetObjectReference<ContextMenu>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _registration;

    private IReadOnlyList<IReadOnlyList<ContextMenuRow>> Sections { get; set; } = [];

    private bool HasToggles => Sections.Any(section => section.Any(row => row.Checked is not null));

    private string AriaLabel =>
        Context.Set == ContextMenuSet.Object ? "Selection actions" : "Canvas actions";

    private string MenuStyle
    {
        get
        {
            var (left, top) = ContextMenuPlacement.Place(
                X,
                Y,
                ContextMenuPlacement.HeightOf(Sections),
                ContainerWidth,
                ContainerHeight
            );
            return FormattableString.Invariant($"left: {left}px; top: {top}px;");
        }
    }

    protected override void OnParametersSet() => Sections = ContextMenuComposition.Compose(Context);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/D12Canvas/ContextMenu.razor.js"
            );
            _dotNetRef = DotNetObjectReference.Create(this);
            _registration = await _jsModule.InvokeAsync<IJSObjectReference>(
                "registerMenu",
                _menuRef,
                _dotNetRef,
                new { openedFromKeyboard = OpenedFromKeyboard }
            );
        }
    }

    [JSInvokable]
    public Task RequestClose() => OnRequestClose.InvokeAsync();

    private static string? CheckedAttribute(ContextMenuRow row) =>
        row.Checked switch
        {
            true => "true",
            false => "false",
            null => null,
        };

    public async ValueTask DisposeAsync()
    {
        if (_registration is not null)
        {
            await _registration.InvokeVoidAsync("dispose");
            await _registration.DisposeAsync();
        }

        if (_jsModule is not null)
        {
            await _jsModule.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }
}
