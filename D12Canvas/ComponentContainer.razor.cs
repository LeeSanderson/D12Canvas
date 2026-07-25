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
            || IsMultiSelected != _lastRenderedIsMultiSelected;
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
        // Only armed when selected-and-not-editing, and only from the container's own body, not a
        // resize handle: a resize handle's mousedown already set _isResizing (it bubbles here
        // afterwards), and !_isResizing keeps this gesture from also engaging on top of that one.
        if (IsSelected && !_editMode && !_isResizing)
        {
            _isMoving = true;
            _moveStart = e;
            _moveStartX = X;
            _moveStartY = Y;
        }

        if (!_editMode)
            return;

        if (!_isResizing)
        {
            _isDragging = true;
            _dragStart = e;
            _startX = X;
            _startY = Y;
        }
    }

    private void HandleMouseMove(MouseEventArgs e)
    {
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

public class ComponentContainerStateChangedEventArgs : EventArgs
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsEditMode { get; set; }
}
