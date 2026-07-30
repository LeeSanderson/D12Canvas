using System.Globalization;
using D12Canvas.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace D12Canvas;

public partial class ComponentContainer : IAsyncDisposable
{
    [Inject]
    private IJSRuntime JavaScriptRuntime { get; set; } = null!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public double X { get; set; }

    [Parameter]
    public double Y { get; set; }

    [Parameter]
    public double Width { get; set; } = 200;

    [Parameter]
    public double Height { get; set; } = 150;

    [Parameter]
    public bool InitialEditMode { get; set; }

    [Parameter]
    public int ZIndex { get; set; }

    [Parameter]
    public string? AccessibleName { get; set; }

    [Parameter]
    public string? Role { get; set; }

    [Parameter]
    public bool IsSelected { get; set; }

    // Ticket 43/75: never rendered directly by ComponentContainer itself (ChildContent stays an
    // opaque RenderFragment) - carried purely so ShouldRender can detect an in-place Props edit at
    // otherwise-unchanged Bounds/selection, which none of its other compared fields would catch.
    [Parameter]
    public object? Props { get; set; }

    // Ticket 33: true when this instance is one of 2+ currently selected together. Its own resize
    // handles are suppressed in that case - the multi-selection's combined bounding box (rendered
    // by DiagramCanvas) grows its own handles instead, so a group resize doesn't collide with 8
    // more handles per member underneath it.
    [Parameter]
    public bool IsMultiSelected { get; set; }

    // Ticket 32: carries the click's shift-key state so DiagramCanvas can toggle this instance's
    // membership in a multi-selection instead of always collapsing to single-select.
    [Parameter]
    public EventCallback<bool> OnSelect { get; set; }

    // Ticket 30: fired once, on release, with the instance's final Bounds - a drag-move is one
    // gesture (ADR 0007's "recorded once on gesture commit, never per intermediate frame"), so
    // Board only needs to hear about the end state, not every intermediate mousemove tick.
    [Parameter]
    public EventCallback<Bounds> OnMoved { get; set; }

    // Ticket 31: same contract as OnMoved but for a handle-drag resize - fired once, on release,
    // with the instance's final Bounds.
    [Parameter]
    public EventCallback<Bounds> OnResized { get; set; }

    // Ticket 48: fired the instant a port is pressed - DiagramCanvas owns the rest of the
    // connector-drag gesture (live preview, drop hit-test) from there, since a completed
    // connection spans two different instances.
    [Parameter]
    public EventCallback<PortDragStartEventArgs> OnPortDragStart { get; set; }

    // Ticket 55/ADR 0005: this instance's own runtime-added ports - instance-scoped state that
    // lives on ComponentInstance itself, passed down the same way Props is.
    [Parameter]
    public IReadOnlyList<PortDef> CustomPorts { get; set; } = Array.Empty<PortDef>();

    // Ticket 55: fired when a double-click on one of the four border strips adds a custom port -
    // DiagramCanvas owns turning this into an undoable AddCustomPortCommand, since it alone knows
    // which ComponentInstance this container renders.
    [Parameter]
    public EventCallback<PortDef> OnAddCustomPort { get; set; }

    [Parameter]
    public EventCallback<ComponentContainerStateChangedEventArgs> OnStateChanged { get; set; }

    [CascadingParameter(Name = "ParentCanvas")]
    private DiagramCanvas? ParentCanvas { get; set; }

    private bool _editMode;
    private bool _isDragging;
    private bool _isResizing;
    private ResizeDirection _currentResizeDirection;
    private MouseEventArgs? _dragStart;
    private double _startX;
    private double _startY;
    private double _startWidth;
    private double _startHeight;

    // A separate gesture from the _editMode-gated _isDragging/_isResizing pair above (legacy,
    // predates the Board-backed canvas - still used standalone by ComponentContainerDemo.razor).
    // This one triggers on a selected instance without needing edit mode at all, and its own
    // state never interacts with the legacy fields - moving a selected instance shouldn't require
    // first entering an editing mode.
    private bool _isMoving;
    private MouseEventArgs? _moveStart;
    private double _moveStartX;
    private double _moveStartY;

    // Ticket 48: true for the span of a single mousedown - set synchronously in StartPortDrag
    // (before OnPortDragStart's async invocation, so it's already true by the time this same
    // mousedown bubbles up from the port) and consumed/cleared immediately in HandleMouseDown.
    // Deliberately not left true for the gesture's whole duration: the connector drag's eventual
    // mouseup can land on a completely different instance, so this container may never see it.
    private bool _isPortDragging;
    private ElementReference _containerRef;
    private DotNetObjectReference<ComponentContainer>? _dotNetRef;
    private IJSObjectReference? _jsModule;

    private double _lastRenderedX;
    private double _lastRenderedY;
    private double _lastRenderedWidth;
    private double _lastRenderedHeight;
    private bool _lastRenderedEditMode;
    private bool _lastRenderedIsSelected;
    private bool _lastRenderedIsMultiSelected;
    private object? _lastRenderedProps;
    private int _lastRenderedZIndex;

    // Ticket 55: CustomPorts is the same mutable List<PortDef> reference across renders (a port is
    // added/undone in place on ComponentInstance), so reference equality can't detect a change -
    // count is a cheap enough proxy since a port is only ever added or removed wholesale, never
    // repositioned in place.
    private int _lastRenderedCustomPortsCount;

    private string ContainerStyle =>
        $"left: {X}px; top: {Y}px; width: {Width}px; height: {Height}px; z-index: {ZIndex};";

    private string ContainerCssClass =>
        IsSelected
            ? $"component-container {(_editMode ? "edit-mode" : "view-mode")} selected"
            : $"component-container {(_editMode ? "edit-mode" : "view-mode")}";

    protected override void OnInitialized()
    {
        _editMode = InitialEditMode;
    }

    protected override bool ShouldRender()
    {
        // Blazor re-invokes every child on the parent's own StateHasChanged (e.g. the
        // canvas panning) regardless of whether this instance's own state changed. Skip
        // the re-render when nothing this component owns has actually changed.
        return X != _lastRenderedX
            || Y != _lastRenderedY
            || Width != _lastRenderedWidth
            || Height != _lastRenderedHeight
            || _editMode != _lastRenderedEditMode
            || IsSelected != _lastRenderedIsSelected
            || IsMultiSelected != _lastRenderedIsMultiSelected
            // Ticket 75: an in-place Props edit (ticket 43's inline text editing, or any future
            // property-panel edit) at unchanged Bounds/selection would otherwise never reach
            // ChildContent - Props types are records, so this is a cheap structural comparison.
            || !Equals(Props, _lastRenderedProps)
            // Ticket 55: a custom port added (or undone) since the last render, at otherwise
            // unchanged Bounds/selection.
            || CustomPorts.Count != _lastRenderedCustomPortsCount
            // Ticket 60: a layering command changes ZIndex alone, at otherwise unchanged
            // Bounds/selection - without this check, a stacking change wouldn't render until some
            // unrelated parameter also changed, violating ADR 0008's "renders immediately."
            || ZIndex != _lastRenderedZIndex;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _lastRenderedX = X;
        _lastRenderedY = Y;
        _lastRenderedWidth = Width;
        _lastRenderedHeight = Height;
        _lastRenderedEditMode = _editMode;
        _lastRenderedIsSelected = IsSelected;
        _lastRenderedIsMultiSelected = IsMultiSelected;
        _lastRenderedProps = Props;
        _lastRenderedCustomPortsCount = CustomPorts.Count;
        _lastRenderedZIndex = ZIndex;

        if (firstRender)
        {
            _jsModule = await JavaScriptRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/D12Canvas/ComponentContainer.razor.js"
            );
            _dotNetRef = DotNetObjectReference.Create(this);
        }
    }

    private void HandleClick(MouseEventArgs e) => OnSelect.InvokeAsync(e.ShiftKey);

    private void HandleMouseDown(MouseEventArgs e)
    {
        // Ticket 48: _isPortDragging only needs to survive from the port's own mousedown handler
        // to this same event's bubble arriving here - captured and cleared immediately so it
        // can't leak into a later, unrelated mousedown on this same container (this container may
        // never see the connector-drag gesture's own eventual mouseup/mousemove at all, since
        // that could land on a completely different instance - see HandleMouseMove/Up below).
        var wasPortDragging = _isPortDragging;
        _isPortDragging = false;

        // Only armed when selected-and-not-editing, and only from the container's own body, not a
        // resize handle or a port: a resize handle's mousedown already set _isResizing (same for
        // a port's own mousedown and wasPortDragging) - each bubbles here afterwards, and the
        // matching guard keeps this gesture from also engaging on top of it.
        if (IsSelected && !_editMode && !_isResizing && !wasPortDragging)
        {
            _isMoving = true;
            _moveStart = e;
            _moveStartX = X;
            _moveStartY = Y;
        }

        if (!_editMode)
            return;

        if (!_isResizing && !wasPortDragging)
        {
            _isDragging = true;
            _dragStart = e;
            _startX = X;
            _startY = Y;
        }
    }

    private void HandleMouseMove(MouseEventArgs e)
    {
        // Ticket 48: while a connector drag is in progress (started here or on any other
        // instance), DiagramCanvas owns the whole gesture - forward raw client coordinates
        // rather than running this container's own move/resize logic. Needed even when the
        // cursor never leaves this container's own bounding box (e.g. still near the source
        // port), since @onmousemove:stopPropagation would otherwise keep DiagramCanvas from ever
        // seeing the event.
        if (ParentCanvas?.IsConnectingPort == true)
        {
            ParentCanvas.UpdatePortDrag(e.ClientX, e.ClientY);
            return;
        }

        if (_isMoving && _moveStart != null)
        {
            var (deltaX, deltaY) = ScaledDelta(_moveStart, e);

            X = _moveStartX + deltaX;
            Y = _moveStartY + deltaY;
            return;
        }

        // Resizing isn't gated by _editMode: a resize-handle mousedown only ever arms _isResizing
        // when its handle actually rendered (IsSelected || _editMode, see the .razor markup), so
        // this branch already can't fire for an instance that's neither selected nor editing.
        if (_isResizing && _dragStart != null)
        {
            var (deltaX, deltaY) = ScaledDelta(_dragStart, e);

            ApplyResize(deltaX, deltaY);
            NotifyStateChanged();
            return;
        }

        if (!_editMode)
            return;

        if (_isDragging && _dragStart != null)
        {
            var (deltaX, deltaY) = ScaledDelta(_dragStart, e);

            X = _startX + deltaX;
            Y = _startY + deltaY;

            NotifyStateChanged();
        }
    }

    // Pan cancels out of a screen-space delta - only the canvas's current zoom scale matters.
    private (double DeltaX, double DeltaY) ScaledDelta(MouseEventArgs from, MouseEventArgs to)
    {
        double deltaX = to.ClientX - from.ClientX;
        double deltaY = to.ClientY - from.ClientY;

        if (ParentCanvas != null)
        {
            deltaX /= ParentCanvas.ZoomPanTracker.Scale;
            deltaY /= ParentCanvas.ZoomPanTracker.Scale;
        }

        return (deltaX, deltaY);
    }

    private void HandleMouseUp(MouseEventArgs e)
    {
        // Ticket 48: same forwarding as HandleMouseMove above - this may be the source
        // instance's own port drag ending on itself, or a drop landing on a different instance's
        // body (its own @onmouseup:stopPropagation would otherwise swallow the release).
        if (ParentCanvas?.IsConnectingPort == true)
        {
            ParentCanvas.CompletePortDrag(e.ClientX, e.ClientY);
            return;
        }

        if (_isMoving)
        {
            _isMoving = false;
            _moveStart = null;

            // Skip the callback entirely for a plain click (mousedown+mouseup with no movement
            // in between) on an already-selected instance - nothing actually moved.
            if (X != _moveStartX || Y != _moveStartY)
            {
                OnMoved.InvokeAsync(new Bounds(X, Y, Width, Height));
            }
        }

        if (_isResizing)
        {
            _isResizing = false;

            // Same no-op-on-no-movement guard as OnMoved above.
            if (X != _startX || Y != _startY || Width != _startWidth || Height != _startHeight)
            {
                OnResized.InvokeAsync(new Bounds(X, Y, Width, Height));
            }
        }

        _isDragging = false;
        _dragStart = null;
    }

    private void StartResize(MouseEventArgs e, ResizeDirection direction)
    {
        _isResizing = true;
        _currentResizeDirection = direction;
        _dragStart = e;
        _startX = X;
        _startY = Y;
        _startWidth = Width;
        _startHeight = Height;

        // Stop the event from propagating to prevent dragging
        // e.StopPropagation();
    }

    // Ticket 48/55: a port's own mousedown (standard or custom - PortRef covers both). Doesn't
    // stop propagation, so it bubbles up to HandleMouseDown afterwards (same ordering the resize
    // handles above rely on) - setting _isPortDragging first stops that handler from also arming
    // an instance move.
    private void StartPortDrag(MouseEventArgs e, PortRef port)
    {
        _isPortDragging = true;
        OnPortDragStart.InvokeAsync(new PortDragStartEventArgs(port, e.ClientX, e.ClientY));
    }

    // Ticket 55/ADR 0005: a double-click anywhere along one of the four border strips (rendered
    // only while ShowSelectionOverlay, see the .razor markup) adds a custom port there. OffsetX/
    // OffsetY (relative to the strip's own box, which spans the container's full width or height
    // on its axis - see .port-strip CSS) gives the fraction along that side directly; the
    // perpendicular fraction is the side's own fixed 0/1, the same border-center convention
    // StandardPorts already uses.
    private void AddCustomPort(MouseEventArgs e, PortStripSide side)
    {
        var (fractionX, fractionY) = side switch
        {
            PortStripSide.Top => (Clamp01(e.OffsetX / Width), 0.0),
            PortStripSide.Right => (1.0, Clamp01(e.OffsetY / Height)),
            PortStripSide.Bottom => (Clamp01(e.OffsetX / Width), 1.0),
            PortStripSide.Left => (0.0, Clamp01(e.OffsetY / Height)),
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };

        OnAddCustomPort.InvokeAsync(new PortDef(fractionX, fractionY));
    }

    private static double Clamp01(double value) => Math.Clamp(value, 0, 1);

    // Ticket 55: the shared visibility gate for the border strips and the resize handles - both
    // are selection-driven overlay affordances, suppressed for a multi-selected member (ticket 33's
    // shared bounding-box overlay grows its own handles instead) but shown during edit mode too,
    // same as resize handles always have been.
    private bool ShowSelectionOverlay => (IsSelected && !IsMultiSelected) || _editMode;

    private static string CustomPortStyle(PortDef port) =>
        $"left: calc({FormatPercent(port.FractionX)}% - 10px); top: calc({FormatPercent(port.FractionY)}% - 10px);";

    private static string FormatPercent(double fraction) =>
        (fraction * 100).ToString(CultureInfo.InvariantCulture);

    private void ApplyResize(double deltaX, double deltaY)
    {
        var resized = ResizeMath.Apply(
            new Bounds(_startX, _startY, _startWidth, _startHeight),
            _currentResizeDirection,
            deltaX,
            deltaY,
            ResizeMath.DefaultMinWidth,
            ResizeMath.DefaultMinHeight
        );

        X = resized.X;
        Y = resized.Y;
        Width = resized.Width;
        Height = resized.Height;
    }

    private void SwitchToEditMode()
    {
        _editMode = true;
        StateHasChanged();
        RegisterClickOutsideHandler();
    }

    private void ExitEditMode()
    {
        if (_editMode)
        {
            _editMode = false;
            StateHasChanged();
            UnregisterClickOutsideHandler();
        }
    }

    [JSInvokable]
    public void OnClickOutside()
    {
        ExitEditMode();
    }

    private async void RegisterClickOutsideHandler()
    {
        if (_jsModule != null && _editMode)
        {
            await _jsModule.InvokeVoidAsync("registerClickOutside", _containerRef, _dotNetRef);
        }
    }

    private async void UnregisterClickOutsideHandler()
    {
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("unregisterClickOutside");
        }
    }

    public async ValueTask DisposeAsync()
    {
        UnregisterClickOutsideHandler();
        _dotNetRef?.Dispose();
        if (_jsModule != null)
        {
            await _jsModule.DisposeAsync();
        }
    }

    private void NotifyStateChanged()
    {
        OnStateChanged.InvokeAsync(
            new ComponentContainerStateChangedEventArgs
            {
                X = X,
                Y = Y,
                Width = Width,
                Height = Height,
                IsEditMode = _editMode,
            }
        );
    }
}

public enum ResizeDirection
{
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left,
}

// Ticket 55: which of the four border strips a double-click-to-add-a-custom-port landed on.
public enum PortStripSide
{
    Top,
    Right,
    Bottom,
    Left,
}

public class ComponentContainerStateChangedEventArgs : EventArgs
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsEditMode { get; set; }
}

public class PortDragStartEventArgs : EventArgs
{
    public PortRef Port { get; }
    public double ClientX { get; }
    public double ClientY { get; }

    public PortDragStartEventArgs(PortRef port, double clientX, double clientY)
    {
        Port = port;
        ClientX = clientX;
        ClientY = clientY;
    }
}
