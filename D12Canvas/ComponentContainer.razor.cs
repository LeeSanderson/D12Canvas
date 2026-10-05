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

    // The board entity this container renders, carried on the root as the classification marker
    // the canvas's pointer listener reads. Null for a container rendered outside a Board.
    [Parameter]
    public Guid? EntityId { get; set; }

    [Parameter]
    public string? AccessibleName { get; set; }

    [Parameter]
    public string? Role { get; set; }

    [Parameter]
    public bool IsSelected { get; set; }

    // False while this instance is a member of a Group - a grouped member is
    // reachable only through the group's own single tab stop, never individually, so it renders
    // with no tabindex at all rather than tabindex="-1" (which would still accept a direct
    // .focus() call, e.g. from stale JS).
    [Parameter]
    public bool Focusable { get; set; } = true;

    // False while this instance is a member of a group, where a press on the author's own content
    // must reach the instance rather than the control, so the canvas's pointer listener classifies
    // it as the instance. Rendered as a marker the listener reads.
    [Parameter]
    public bool Addressable { get; set; } = true;

    // Never rendered directly by ComponentContainer itself (ChildContent stays an
    // opaque RenderFragment) - carried purely so ShouldRender can detect an in-place Props edit at
    // otherwise-unchanged Bounds/selection, which none of its other compared fields would catch.
    [Parameter]
    public object? Props { get; set; }

    // True when this instance is one of 2+ currently selected together. Its own resize
    // handles are suppressed in that case - the multi-selection's combined bounding box (rendered
    // by DiagramCanvas) grows its own handles instead, so a group resize doesn't collide with 8
    // more handles per member underneath it.
    [Parameter]
    public bool IsMultiSelected { get; set; }

    // Fired when this container's own root element receives DOM focus - native Tab/Shift+Tab
    // navigation lands here with no keyboard wiring of our own needed (see Focusable's tabindex),
    // so this is the sole entry point for the "focusing selects" half of focus-follows-selection.
    // DiagramCanvas always resolves it to a single-entity select, never an add.
    [Parameter]
    public EventCallback OnFocus { get; set; }

    // This instance's own runtime-added ports - instance-scoped state that
    // lives on ComponentInstance itself, passed down the same way Props is.
    [Parameter]
    public IReadOnlyList<PortDef> CustomPorts { get; set; } = Array.Empty<PortDef>();

    // Which of this instance's four standard ports (if any) DiagramCanvas's keyboard
    // connector-attachment gesture currently has highlighted - either mid-pick, or already armed as
    // that connection's source and waiting on a target chosen elsewhere. Null (the common case)
    // renders no highlight. A highlighted custom port instead comes through FocusedCustomPortId
    // below - the two are mutually exclusive (see DiagramCanvas.FocusedPortEndpointFor).
    [Parameter]
    public PortId? FocusedPortId { get; set; }

    // The custom-port counterpart to FocusedPortId above - the Id of one of this instance's own
    // CustomPorts entries, or null.
    [Parameter]
    public Guid? FocusedCustomPortId { get; set; }

    [Parameter]
    public EventCallback<ComponentContainerStateChangedEventArgs> OnStateChanged { get; set; }

    [CascadingParameter(Name = "ParentCanvas")]
    private DiagramCanvas? ParentCanvas { get; set; }

    private bool _editMode;
    private bool _isDragging;
    private MouseEventArgs? _dragStart;
    private double _startX;
    private double _startY;

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
    private bool _lastRenderedFocusable;
    private bool _lastRenderedAddressable;
    private object? _lastRenderedProps;
    private int _lastRenderedZIndex;

    // CustomPorts is the same mutable List<PortDef> reference across renders (a port is
    // added/undone in place on ComponentInstance), so reference equality can't detect a change -
    // count is a cheap enough proxy since a port is only ever added or removed wholesale, never
    // repositioned in place.
    private int _lastRenderedCustomPortsCount;

    private PortId? _lastRenderedFocusedPortId;
    private Guid? _lastRenderedFocusedCustomPortId;

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
            // A Group/Ungroup command flips Focusable (and so the rendered tabindex) alone, at
            // otherwise-unchanged Bounds/selection (grouping keeps every member selected) - without
            // this check the tabindex attribute wouldn't update until some unrelated parameter also
            // changed.
            || Focusable != _lastRenderedFocusable
            || Addressable != _lastRenderedAddressable
            // An in-place Props edit (inline text editing, or any future
            // property-panel edit) at unchanged Bounds/selection would otherwise never reach
            // ChildContent - Props types are records, so this is a cheap structural comparison.
            || !Equals(Props, _lastRenderedProps)
            // A custom port added (or undone) since the last render, at otherwise
            // unchanged Bounds/selection.
            || CustomPorts.Count != _lastRenderedCustomPortsCount
            // A layering command changes ZIndex alone, at otherwise unchanged
            // Bounds/selection - without this check, a stacking change wouldn't render until some
            // unrelated parameter also changed, when it should render immediately.
            || ZIndex != _lastRenderedZIndex
            // The keyboard connector-attachment gesture changes which port is highlighted
            // (an arrow-key/Space pick, or arming/confirming a source) at otherwise unchanged
            // Bounds/selection - without this check the highlight wouldn't move until some
            // unrelated parameter also changed.
            || FocusedPortId != _lastRenderedFocusedPortId
            || FocusedCustomPortId != _lastRenderedFocusedCustomPortId;
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
        _lastRenderedFocusable = Focusable;
        _lastRenderedAddressable = Addressable;
        _lastRenderedProps = Props;
        _lastRenderedCustomPortsCount = CustomPorts.Count;
        _lastRenderedZIndex = ZIndex;
        _lastRenderedFocusedPortId = FocusedPortId;
        _lastRenderedFocusedCustomPortId = FocusedCustomPortId;

        if (firstRender)
        {
            _jsModule = await JavaScriptRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/D12Canvas/ComponentContainer.razor.js"
            );
            _dotNetRef = DotNetObjectReference.Create(this);
        }
    }

    private Task HandleFocus() => OnFocus.InvokeAsync();

    private void HandleMouseDown(MouseEventArgs e)
    {
        if (!_editMode)
            return;

        _isDragging = true;
        _dragStart = e;
        _startX = X;
        _startY = Y;
    }

    private void HandleMouseMove(MouseEventArgs e)
    {
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
        _isDragging = false;
        _dragStart = null;
    }

    // A standard port's own CSS class, plus the highlight class while the keyboard
    // connector-attachment gesture has this exact port as FocusedPortId. The side class is derived
    // from portId itself rather than taken as a separate parameter, so a call site can't hand in a
    // mismatched pair.
    private string StandardPortCssClass(PortId portId) =>
        FocusedPortId == portId
            ? $"port {StandardPortSideClass(portId)} port-focused"
            : $"port {StandardPortSideClass(portId)}";

    private static string StandardPortSideClass(PortId portId) =>
        portId switch
        {
            PortId.Top => "port-top",
            PortId.Right => "port-right",
            PortId.Bottom => "port-bottom",
            PortId.Left => "port-left",
            _ => throw new ArgumentOutOfRangeException(nameof(portId)),
        };

    // The custom-port counterpart to StandardPortCssClass - every custom port already renders
    // via the single shared "custom-port" class (CustomPortStyle positions each one individually by
    // inline style, not a per-port CSS class), so only the highlight needs conditional logic here.
    private string CustomPortCssClass(Guid portId) =>
        FocusedCustomPortId == portId ? "port custom-port port-focused" : "port custom-port";

    // The shared visibility gate for the border strips and the resize handles - both
    // are selection-driven overlay affordances, suppressed for a multi-selected member (the
    // shared bounding-box overlay grows its own handles instead) but shown during edit mode too,
    // same as resize handles always have been.
    private bool ShowSelectionOverlay => (IsSelected && !IsMultiSelected) || _editMode;

    private static string CustomPortStyle(PortDef port) =>
        $"left: calc({FormatPercent(port.FractionX)}% - 10px); top: calc({FormatPercent(port.FractionY)}% - 10px);";

    private static string FormatPercent(double fraction) =>
        (fraction * 100).ToString(CultureInfo.InvariantCulture);

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
