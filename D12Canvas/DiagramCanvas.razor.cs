using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class DiagramCanvas : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Inject]
    private IComponentRegistry Registry { get; set; } = null!;

    // Sized to absorb the pan-render throttle's worst case: at PanRenderInterval (16ms) and a
    // fast drag, the pointer can move well past this before the next windowed re-render lands,
    // but a much larger margin just inflates mount/unmount cost for no visible benefit.
    public const double DefaultOverscan = 200;

    [Parameter]
    public Board? Board { get; set; }

    [Parameter]
    public double Overscan { get; set; } = DefaultOverscan;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public EventCallback<ZoomPanChangedEventArgs> OnZoomOrPanChanged { get; set; }
    public event EventHandler<ZoomPanChangedEventArgs>? ZoomOrPanChanged;

    // A palette entry has no compile-time-typed payload it can hand across the native HTML5 drag
    // session (Blazor's DragEventArgs.DataTransfer exposes no SetData/GetData) - Palette instead
    // calls this directly (via its explicit Canvas reference, ADR 0002) to stash which type is
    // being dragged, for HandleDrop to read back once the gesture completes.
    private string? _pendingPaletteDragKey;
    private bool _isDragOverBoard;

    public void BeginPaletteDrag(string componentTypeKey) =>
        _pendingPaletteDragKey = componentTypeKey;

    // ADR 0009: click-to-add's default position is the viewport center (not a fixed board-space
    // origin, which could land off-screen once the user has panned), with a small cascading
    // offset per successive click so repeated adds don't stack in a perfectly overlapping pile.
    // The counter is global (not per component type) - it only needs to keep successive placements
    // visually apart, and never resets, matching this app's momentary-gesture-only model (there's
    // no "start a new placement session" event to reset it on).
    private const double ClickToAddCascadeStep = 20;
    private int _clickToAddCascadeCount;

    // Selection is transient view state (ADR 0006) - it lives here, never on Board, and is never
    // serialized or tracked by undo/redo. Ticket 32: ad-hoc multi-select via marquee/shift-click.
    private readonly HashSet<Guid> _selectedInstanceIds = new();

    private readonly ZoomPanTracker _zoomPanTracker = new ZoomPanTracker();

    // Make ZoomPanTracker accessible to child components
    public ZoomPanTracker ZoomPanTracker => _zoomPanTracker;

    // Ticket 32: a plain drag on empty canvas still pans (pre-existing behaviour, unchanged);
    // Shift+drag draws an intersection-based marquee instead, pairing with Shift-click's existing
    // "multi-select gesture" meaning. A drag starting inside the current selection's own combined
    // bounding box does neither - reserved for ticket 33's group-move-as-a-unit, so for now it's
    // deliberately inert. Whichever of the three a gesture turns out to be, it starts and ends with
    // mousedown/mouseup on the same element, so the browser's native click fires right after it -
    // without the _dragMoved guard that click would immediately clear the selection the drag just
    // established (or leave alone), the same wrinkle ticket 29's original pan guard existed to solve.
    private bool _isPanning;
    private bool _isMarqueeSelecting;
    private bool _isWithinSelectionDrag;
    private bool _dragMoved;
    private MouseEventArgs? _panStart;
    private DateTime _lastPanRender = DateTime.MinValue;
    private static readonly TimeSpan PanRenderInterval = TimeSpan.FromMilliseconds(16); // ~60fps cap
    private (double Left, double Top) _marqueeContainerOrigin;
    private (double X, double Y) _marqueeAnchor;
    private (double X, double Y) _marqueeCurrent;
    private ElementReference ContainerElement;
    private DotNetObjectReference<DiagramCanvas>? _dotNetObjectRef;
    private List<Action> _cleanupFunctions = new List<Action>();
    private IJSObjectReference? _jsModule;

    protected override void OnInitialized()
    {
        _zoomPanTracker.Changed += OnZoomPanChanged;
        _dotNetObjectRef = DotNetObjectReference.Create(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JS.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/D12Canvas/DiagramCanvas.razor.js"
            );

            var dimensions = await _jsModule.InvokeAsync<Dictionary<string, double>>(
                "getContainerDimensions",
                ContainerElement
            );

            _zoomPanTracker.SetContainerSize((int)dimensions["width"], (int)dimensions["height"]);
            _zoomPanTracker.SetCanvasSize(3000, 3000);

            // Set up resize listener
            var resizeCleanup = await _jsModule.InvokeAsync<Action>(
                "addResizeListener",
                ContainerElement,
                _dotNetObjectRef
            );

            // Set up keyboard listener
            var keyboardCleanup = await _jsModule.InvokeAsync<Action>(
                "addKeyboardListener",
                ContainerElement,
                _dotNetObjectRef
            );

            // Store cleanup functions for disposal
            _cleanupFunctions.Add(resizeCleanup);
            _cleanupFunctions.Add(keyboardCleanup);

            StateHasChanged();
        }
    }

    [JSInvokable]
    public void OnContainerResized(double width, double height)
    {
        _zoomPanTracker.SetContainerSize((int)width, (int)height);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnZoomIn()
    {
        _zoomPanTracker.ZoomIn();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnZoomOut()
    {
        _zoomPanTracker.ZoomOut();
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPanLeft()
    {
        _zoomPanTracker.Pan(50, 0);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPanRight()
    {
        _zoomPanTracker.Pan(-50, 0);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPanUp()
    {
        _zoomPanTracker.Pan(0, 50);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnPanDown()
    {
        _zoomPanTracker.Pan(0, -50);
        StateHasChanged();
    }

    // ADR 0009: Escape clears the selection (later, it will also cancel an in-progress connector
    // drag - no such gesture exists yet, so clearing selection is its whole job today).
    [JSInvokable]
    public void OnEscapePressed()
    {
        _selectedInstanceIds.Clear();
        StateHasChanged();
    }

    private bool IsSelected(Guid instanceId) => _selectedInstanceIds.Contains(instanceId);

    // Ticket 32: a shift-click toggles the clicked instance's membership without disturbing the
    // rest of the selection; a plain click always collapses the selection down to just this one.
    private void SelectComponent(Guid instanceId, bool addToSelection)
    {
        if (addToSelection)
        {
            if (!_selectedInstanceIds.Remove(instanceId))
            {
                _selectedInstanceIds.Add(instanceId);
            }
            return;
        }

        _selectedInstanceIds.Clear();
        _selectedInstanceIds.Add(instanceId);
    }

    // Ticket 30: fired once by ComponentContainer's OnMoved, on release - the whole
    // press-to-release drag is one gesture, so Board is only ever mutated with the final Bounds,
    // never per intermediate mousemove tick.
    private void MoveComponent(Guid instanceId, Bounds bounds)
    {
        var instance = Board?.GetComponent(instanceId);
        if (instance is null)
        {
            return;
        }

        instance.Bounds = bounds;
        StateHasChanged();
    }

    // Ticket 31: same shape as MoveComponent, fired once by ComponentContainer's OnResized.
    private void ResizeComponent(Guid instanceId, Bounds bounds)
    {
        var instance = Board?.GetComponent(instanceId);
        if (instance is null)
        {
            return;
        }

        instance.Bounds = bounds;
        StateHasChanged();
    }

    // Bound directly to the canvas background's own click, so it never fires for a click that
    // landed on a ComponentContainer (that element stops the click from propagating here).
    private void HandleCanvasClick()
    {
        if (_dragMoved)
        {
            _dragMoved = false;
            return;
        }

        _selectedInstanceIds.Clear();
    }

    // The registered TComponent's props parameter is a fixed contract (ADR 0001 addendum):
    // [Parameter] public TProps Props { get; set; }
    private const string PropsParameterName = "Props";

    private static IDictionary<string, object> GetComponentParameters(ComponentInstance instance) =>
        new Dictionary<string, object> { [PropsParameterName] = instance.Props };

    // Recomputed whenever DiagramCanvas re-renders. That's driven entirely by the pan/zoom/
    // resize events that already call StateHasChanged (throttled for pan, see HandleMouseMove) -
    // never by a per-frame timer - so the mounted window follows the same cadence.
    private IReadOnlyCollection<ComponentInstance> VisibleComponents =>
        Board?.GetVisible(_zoomPanTracker.Viewport, Overscan) ?? Array.Empty<ComponentInstance>();

    private string CanvasCssClass =>
        _isDragOverBoard ? "diagram-canvas drag-over" : "diagram-canvas";

    // Same handler for both events - dragenter and dragover mark the same "still hovering" state.
    private void HandleDragEnterOrOver(DragEventArgs e) => _isDragOverBoard = true;

    private void HandleDragLeave(DragEventArgs e) => _isDragOverBoard = false;

    private string ContentStyle =>
        $"width: {_zoomPanTracker.CanvasWidth}px; height: {_zoomPanTracker.CanvasHeight}px; transform: translate({_zoomPanTracker.PanX}px, {_zoomPanTracker.PanY}px) scale({_zoomPanTracker.Scale});";

    private async Task HandleMouseDown(MouseEventArgs e)
    {
        if (e.Button != 0) // Left mouse button only
        {
            return;
        }

        _dragMoved = false;

        // Fetched fresh rather than reused from first render - same reasoning as HandleDrop: the
        // container can move on the page (scroll, sibling layout changes) between renders.
        var containerRect = await _jsModule!.InvokeAsync<Dictionary<string, double>>(
            "getContainerDimensions",
            ContainerElement
        );
        _marqueeContainerOrigin = (containerRect["left"], containerRect["top"]);
        var boardPoint = ToBoardPoint(e, _marqueeContainerOrigin);

        if (e.ShiftKey)
        {
            _isMarqueeSelecting = true;
            _marqueeAnchor = boardPoint;
            _marqueeCurrent = boardPoint;
            return;
        }

        if (PointIsWithinSelectionBounds(boardPoint))
        {
            // Reserved for ticket 33 (multi-selection moves and resizes as one unit) - left
            // deliberately inert for now: no pan underneath the selection, no new marquee. Tracked
            // separately (rather than just returning) so HandleMouseMove can still mark the
            // gesture as having moved, keeping the trailing click from clearing the selection.
            _isWithinSelectionDrag = true;
            return;
        }

        _isPanning = true;
        _panStart = e;
    }

    private void HandleMouseMove(MouseEventArgs e)
    {
        if (_isMarqueeSelecting)
        {
            _marqueeCurrent = ToBoardPoint(e, _marqueeContainerOrigin);
            _dragMoved = true;
            UpdateMarqueeSelection();
            StateHasChanged();
            return;
        }

        if (_isWithinSelectionDrag)
        {
            _dragMoved = true;
            return;
        }

        if (_isPanning && _panStart != null)
        {
            var deltaX = e.ClientX - _panStart.ClientX;
            var deltaY = e.ClientY - _panStart.ClientY;

            // Pan state updates every tick so no motion is lost; the render itself is
            // throttled since it's what cascades into re-rendering every mounted child.
            _zoomPanTracker.Pan(deltaX, deltaY);
            _dragMoved = true;
            _panStart = e;

            var now = DateTime.UtcNow;
            if (now - _lastPanRender >= PanRenderInterval)
            {
                _lastPanRender = now;
                StateHasChanged();
            }
        }
    }

    private void HandleMouseUp(MouseEventArgs e)
    {
        _isPanning = false;
        _isMarqueeSelecting = false;
        _isWithinSelectionDrag = false;
        _panStart = null;
        // Flush so the view can't be left visually behind a throttled final pan tick.
        StateHasChanged();
    }

    // Screen (client) coordinates to board space, given the container's own page position -
    // shared by the marquee gesture and HandleDrop below.
    private (double X, double Y) ToBoardPoint(
        MouseEventArgs e,
        (double Left, double Top) containerOrigin
    ) =>
        (
            (e.ClientX - containerOrigin.Left - _zoomPanTracker.PanX) / _zoomPanTracker.Scale,
            (e.ClientY - containerOrigin.Top - _zoomPanTracker.PanY) / _zoomPanTracker.Scale
        );

    private static Bounds MarqueeBoundsFrom((double X, double Y) a, (double X, double Y) b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    // Replaces the selection outright with whatever the marquee currently intersects (ADR 0006:
    // intersection semantics, not full-containment) - not additive, so a marquee drag that ends up
    // over nothing empties the selection, the same as clicking empty canvas.
    private void UpdateMarqueeSelection()
    {
        if (Board is null)
        {
            return;
        }

        var marquee = MarqueeBoundsFrom(_marqueeAnchor, _marqueeCurrent);
        _selectedInstanceIds.Clear();
        foreach (var instance in Board.Components)
        {
            if (instance.Bounds.Intersects(marquee))
            {
                _selectedInstanceIds.Add(instance.Id);
            }
        }
    }

    // The combined bounding box of the current selection, or null when nothing is selected - used
    // to keep a plain drag starting there from panning underneath it (reserved for ticket 33).
    private Bounds? SelectedInstancesBounds()
    {
        if (Board is null)
        {
            return null;
        }

        double minX = 0,
            minY = 0,
            maxX = 0,
            maxY = 0;
        var any = false;
        foreach (var instance in Board.Components)
        {
            if (!_selectedInstanceIds.Contains(instance.Id))
            {
                continue;
            }

            minX = any ? Math.Min(minX, instance.Bounds.X) : instance.Bounds.X;
            minY = any ? Math.Min(minY, instance.Bounds.Y) : instance.Bounds.Y;
            maxX = any ? Math.Max(maxX, instance.Bounds.Right) : instance.Bounds.Right;
            maxY = any ? Math.Max(maxY, instance.Bounds.Bottom) : instance.Bounds.Bottom;
            any = true;
        }

        return any ? new Bounds(minX, minY, maxX - minX, maxY - minY) : null;
    }

    private bool PointIsWithinSelectionBounds((double X, double Y) point) =>
        SelectedInstancesBounds()?.Intersects(new Bounds(point.X, point.Y, 0, 0)) ?? false;

    private string MarqueeStyle
    {
        get
        {
            var bounds = MarqueeBoundsFrom(_marqueeAnchor, _marqueeCurrent);
            return $"left: {bounds.X}px; top: {bounds.Y}px; width: {bounds.Width}px; height: {bounds.Height}px;";
        }
    }

    // Matches ComponentContainer's own default Width/Height parameter values - used only when a
    // registration was declared without a DefaultSize (ComponentSize? is optional).
    private const double FallbackWidth = 200;
    private const double FallbackHeight = 150;

    private async Task HandleDrop(DragEventArgs e)
    {
        var componentTypeKey = _pendingPaletteDragKey;
        _pendingPaletteDragKey = null;
        _isDragOverBoard = false;

        if (componentTypeKey is null || Board is null)
        {
            StateHasChanged();
            return;
        }

        // Fetched fresh rather than reused from first render: the container can move on the page
        // (scroll, sibling layout changes) without firing the resize listener that only tracks size.
        var containerRect = await _jsModule!.InvokeAsync<Dictionary<string, double>>(
            "getContainerDimensions",
            ContainerElement
        );

        var (boardX, boardY) = ToBoardPoint(e, (containerRect["left"], containerRect["top"]));

        // The drop point is the center of the placed instance, not its top-left corner - matching
        // where the user's cursor (and the browser's default drag ghost) actually is on release.
        PlaceComponent(componentTypeKey, boardX, boardY);

        StateHasChanged();
    }

    public void ClickToAdd(string componentTypeKey)
    {
        if (Board is null)
        {
            return;
        }

        var offset = _clickToAddCascadeCount * ClickToAddCascadeStep;
        _clickToAddCascadeCount++;

        var viewport = _zoomPanTracker.Viewport;
        PlaceComponent(
            componentTypeKey,
            viewport.X + viewport.Width / 2 + offset,
            viewport.Y + viewport.Height / 2 + offset
        );

        StateHasChanged();
    }

    // Shared by HandleDrop and ClickToAdd - both place a new instance centered on a board-space
    // point, differing only in how that point is derived. Callers are trusted to have already
    // checked Board is non-null (both do, before computing their center point).
    private void PlaceComponent(string componentTypeKey, double centerX, double centerY)
    {
        var registration = Registry.Resolve(componentTypeKey);
        var size = registration.DefaultSize ?? new ComponentSize(FallbackWidth, FallbackHeight);

        Board!.AddComponent(
            new ComponentInstance(
                registration.Key,
                registration.DefaultProps,
                new Bounds(
                    centerX - size.Width / 2,
                    centerY - size.Height / 2,
                    size.Width,
                    size.Height
                )
            )
        );
    }

    private void HandleMouseWheel(WheelEventArgs e)
    {
        var zoomIn = e.DeltaY < 0;
        var zoomed = _zoomPanTracker.Zoom(zoomIn);
        if (zoomed)
        {
            StateHasChanged();
        }
    }

    private void OnZoomPanChanged(object? sender, ZoomPanChangedEventArgs e)
    {
        OnZoomOrPanChanged.InvokeAsync(e);
        ZoomOrPanChanged?.Invoke(this, e);
    }

    public async ValueTask DisposeAsync()
    {
        _cleanupFunctions.ForEach(f => f());
        _cleanupFunctions.Clear();

        _zoomPanTracker.Changed -= OnZoomPanChanged;
        _dotNetObjectRef?.Dispose();

        if (_jsModule != null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}
