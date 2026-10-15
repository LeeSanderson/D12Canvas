using D12Canvas.Model;
using D12Canvas.Pointer;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace D12Canvas;

// Host-placed chrome showing every instance as a plain box and the canvas's viewport as a rect,
// mapped from the union of the content extent and that viewport by the minimap's own tracker,
// through the same framing the viewport commands use. A press here is the canvas's MinimapPan,
// reported through the shared pointer listener with classification off. While it is held the
// mapping stands still, so the board point under the pointer cannot slide as the viewport moves.
public partial class Minimap : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Parameter]
    public DiagramCanvas? Canvas { get; set; }

    private readonly ZoomPanTracker _tracker = new();
    private readonly List<IJSObjectReference> _cleanupHandles = [];
    private ElementReference _root;
    private DotNetObjectReference<Minimap>? _dotNetObjectRef;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _pointerListener;
    private DiagramCanvas? _subscribedCanvas;
    private DiagramCanvas? _listeningFor;
    private int _revision;
    private (Board Board, int Revision, Bounds? Extent)? _extent;
    private bool _disposed;

    // The canvas holding this minimap's press, which keeps the mapping still until it ends.
    private DiagramCanvas? _panningOn;

    private bool Panning => _panningOn is not null;

    private Board? Board => Canvas?.Board;

    protected override void OnInitialized() =>
        _dotNetObjectRef = DotNetObjectReference.Create(this);

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(Canvas, _subscribedCanvas))
        {
            Unsubscribe();
            if (Canvas is not null)
            {
                Canvas.ZoomOrPanChanged += OnCanvasViewportChanged;
                Canvas.Changed += OnCanvasBoardChanged;
                Canvas.BoardReplaced += OnCanvasBoardChanged;
            }

            _subscribedCanvas = Canvas;
        }

        Reframe();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/D12Canvas/DiagramCanvas.razor.js"
            );
            _cleanupHandles.Add(
                await _jsModule.InvokeAsync<IJSObjectReference>(
                    "addResizeListener",
                    _root,
                    _dotNetObjectRef
                )
            );
        }

        if (
            _jsModule is not null
            && !ReferenceEquals(Canvas, _listeningFor)
            && Canvas?.MinimapFocusTarget.Id is not null
        )
        {
            await ListenForPressesAsync(_jsModule);
        }
    }

    private async Task ListenForPressesAsync(IJSObjectReference module)
    {
        if (_pointerListener is not null)
        {
            AbandonPress();
            _cleanupHandles.Remove(_pointerListener);
            await DisposeHandleAsync(_pointerListener);
            _pointerListener = null;
        }

        _listeningFor = Canvas;
        if (Canvas is null)
        {
            return;
        }

        var listener = await module.InvokeAsync<IJSObjectReference>(
            "addPointerListener",
            _root,
            _root,
            _dotNetObjectRef,
            new
            {
                classify = false,
                dragThreshold = ScreenPixels.DragThreshold,
                focusTarget = Canvas.MinimapFocusTarget,
            }
        );
        if (_disposed)
        {
            await DisposeHandleAsync(listener);
            return;
        }

        _pointerListener = listener;
        _cleanupHandles.Add(listener);
    }

    [JSInvokable]
    public void OnContainerResized(double width, double height)
    {
        _tracker.SetContainerSize((int)width, (int)height);
        Reframe();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPointerPressed(PointerPress press)
    {
        if (
            Canvas is not null
            && !Panning
            && Canvas.BeginMinimapPan(press, ToBoardPoint, _pointerListener)
        )
        {
            _panningOn = Canvas;
        }
    }

    [JSInvokable]
    public void OnPointerMoved(PointerMove move)
    {
        _panningOn?.OnPointerMoved(move);
    }

    [JSInvokable]
    public void OnPointerReleased(PointerRelease release) =>
        EndPress(canvas => canvas.OnPointerReleased(release));

    [JSInvokable]
    public void OnPointerCancelled(string reason) =>
        EndPress(canvas => canvas.OnPointerCancelled(reason));

    // The map is let go before the canvas hears the end, so a click's flight reframes it.
    private void EndPress(Action<DiagramCanvas> tellCanvas)
    {
        if (_panningOn is not { } canvas)
        {
            return;
        }

        _panningOn = null;
        tellCanvas(canvas);
        Reframe();
        StateHasChanged();
    }

    // The listener going away ends its press without a release, so the canvas cancels it rather
    // than holding the board for a release that will never arrive.
    private void AbandonPress()
    {
        if (_panningOn is { } canvas)
        {
            _panningOn = null;
            canvas.OnPointerCancelled("minimap-disposed");
        }
    }

    private (double X, double Y) ToBoardPoint(double x, double y) =>
        ((x - _tracker.PanX) / _tracker.Scale, (y - _tracker.PanY) / _tracker.Scale);

    private void OnCanvasViewportChanged(object? sender, ZoomPanChangedEventArgs e)
    {
        Reframe();
        StateHasChanged();
    }

    private void OnCanvasBoardChanged(object? sender, EventArgs e)
    {
        _revision++;
        Reframe();
        StateHasChanged();
    }

    private void Reframe()
    {
        if (
            Panning
            || Canvas is not { Board: { } board } canvas
            || !canvas.ZoomPanTracker.HasKnownContainerSize
        )
        {
            return;
        }

        var viewport = canvas.ZoomPanTracker.Viewport;
        _tracker.Frame(
            ExtentOf(board) is { } extent ? Bounds.Union([extent, viewport])!.Value : viewport
        );
    }

    private Bounds? ExtentOf(Board board)
    {
        if (_extent is not { } cached || cached.Board != board || cached.Revision != _revision)
        {
            cached = (board, _revision, board.ContentExtent());
            _extent = cached;
        }

        return cached.Extent;
    }

    private bool Shown =>
        Board is not null
        && _tracker.HasKnownContainerSize
        && Canvas!.ZoomPanTracker.HasKnownContainerSize;

    private string ContentStyle =>
        FormattableString.Invariant(
            $"transform: translate({_tracker.PanX}px, {_tracker.PanY}px) scale({_tracker.Scale})"
        );

    private string ViewportStyle
    {
        get
        {
            var viewport = Canvas!.ZoomPanTracker.Viewport;
            var scale = _tracker.Scale;
            return FormattableString.Invariant(
                $"left: {viewport.X * scale + _tracker.PanX}px; top: {viewport.Y * scale + _tracker.PanY}px; width: {viewport.Width * scale}px; height: {viewport.Height * scale}px"
            );
        }
    }

    private void Unsubscribe()
    {
        if (_subscribedCanvas is not null)
        {
            _subscribedCanvas.ZoomOrPanChanged -= OnCanvasViewportChanged;
            _subscribedCanvas.Changed -= OnCanvasBoardChanged;
            _subscribedCanvas.BoardReplaced -= OnCanvasBoardChanged;
        }
    }

    private static async Task DisposeHandleAsync(IJSObjectReference handle)
    {
        await handle.InvokeVoidAsync("dispose");
        await handle.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        AbandonPress();
        Unsubscribe();
        foreach (var handle in _cleanupHandles)
        {
            await DisposeHandleAsync(handle);
        }

        _cleanupHandles.Clear();
        _dotNetObjectRef?.Dispose();
        if (_jsModule is not null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}
