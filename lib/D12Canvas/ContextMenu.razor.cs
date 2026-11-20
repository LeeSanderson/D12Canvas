using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace D12Canvas;

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

    [Parameter]
    public bool OpenedFromKeyboard { get; set; }

    [Parameter]
    public EventCallback<ContextMenuCommand> OnInvoke { get; set; }

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
            var box = ContextMenuPlacement.Fit(
                X,
                Y,
                ContextMenuPlacement.HeightOf(Sections),
                ContainerWidth,
                ContainerHeight
            );
            var style = FormattableString.Invariant($"left: {box.Left}px; top: {box.Top}px;");
            if (box.Width is { } width)
            {
                style += FormattableString.Invariant($" width: {width}px;");
            }

            if (box.MaxHeight is { } maxHeight)
            {
                style += FormattableString.Invariant(
                    $" max-height: {maxHeight}px; overflow-y: auto;"
                );
            }

            return style;
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
