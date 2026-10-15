using System.Globalization;
using D12Canvas.Model;
using Microsoft.AspNetCore.Components;

namespace D12Canvas;

public partial class ComponentContainer
{
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
    public int ZIndex { get; set; }

    // The board entity this container renders, carried on the root as the classification marker
    // the canvas's pointer listener reads. Null for a container rendered outside a Board.
    [Parameter]
    public Guid? EntityId { get; set; }

    // A locked instance is marked so a primary press passes it by, and shows no affordance: no
    // resize handle and no port, so nothing is pulled from it and nothing is dropped on it.
    [Parameter]
    public bool Locked { get; set; }

    // False for an instance that is not on the board yet, a clone drag's copy, which renders no hit
    // marker so a press passes through it.
    [Parameter]
    public bool HasHitMarker { get; set; } = true;

    [Parameter]
    public string? AccessibleName { get; set; }

    [Parameter]
    public string? Role { get; set; }

    [Parameter]
    public bool IsSelected { get; set; }

    // False while this instance is not addressable, inside a group that is not entered - such a
    // member is reachable only through its group's tab stop, never individually, so it renders
    // with no tabindex at all rather than tabindex="-1" (which would still accept a direct
    // .focus() call, e.g. from stale JS).
    [Parameter]
    public bool Focusable { get; set; } = true;

    // False while this instance is inside a group that is not entered, where a press on the
    // author's own content must reach the instance rather than the control, so the canvas's
    // pointer listener classifies it as the instance. Rendered as a marker the listener reads.
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

    // The keyboard pick is an auto endpoint on this instance, which highlights all four standard
    // ports: the canvas chooses among them, and never a custom port.
    [Parameter]
    public bool AutoPortFocused { get; set; }

    // Where keyboard port placement has the port it would add, as fractions of the bounds; drawn as
    // a hollow dot and not yet one of CustomPorts.
    [Parameter]
    public (double FractionX, double FractionY)? ProvisionalPort { get; set; }

    // The canvas's zoom, which the border partition reads because its spans are measured in screen
    // pixels.
    [Parameter]
    public double Scale { get; set; } = 1;

    // True while a connector drag would attach to this instance if released now, which shows its
    // ports as it would on a selected instance.
    [Parameter]
    public bool IsDropTarget { get; set; }

    private IReadOnlyList<BorderSpan> _partition = [];

    private double _lastRenderedX;
    private double _lastRenderedY;
    private double _lastRenderedWidth;
    private double _lastRenderedHeight;
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
    private bool _lastRenderedAutoPortFocused;
    private (double FractionX, double FractionY)? _lastRenderedProvisionalPort;
    private double _lastRenderedScale;
    private bool _lastRenderedIsDropTarget;
    private bool _lastRenderedLocked;
    private bool _lastRenderedHasHitMarker;

    private string ContainerStyle =>
        $"left: {X}px; top: {Y}px; width: {Width}px; height: {Height}px; z-index: {ZIndex};";

    private string ContainerCssClass =>
        IsSelected ? "component-container selected" : "component-container";

    protected override bool ShouldRender()
    {
        // Blazor re-invokes every child on the parent's own StateHasChanged (e.g. the
        // canvas panning) regardless of whether this instance's own state changed. Skip
        // the re-render when nothing this component owns has actually changed.
        return X != _lastRenderedX
            || Y != _lastRenderedY
            || Width != _lastRenderedWidth
            || Height != _lastRenderedHeight
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
            || FocusedCustomPortId != _lastRenderedFocusedCustomPortId
            || AutoPortFocused != _lastRenderedAutoPortFocused
            || ProvisionalPort != _lastRenderedProvisionalPort
            || IsDropTarget != _lastRenderedIsDropTarget
            || Locked != _lastRenderedLocked
            || HasHitMarker != _lastRenderedHasHitMarker
            // The partition is measured in screen pixels, so a zoom can add or drop a span.
            || (ShowPorts && Scale != _lastRenderedScale);
    }

    protected override void OnParametersSet() =>
        _partition = ShowPorts ? BorderPartition.Of(Width, Height, Scale, CustomPorts) : [];

    protected override void OnAfterRender(bool firstRender)
    {
        _lastRenderedX = X;
        _lastRenderedY = Y;
        _lastRenderedWidth = Width;
        _lastRenderedHeight = Height;
        _lastRenderedIsSelected = IsSelected;
        _lastRenderedIsMultiSelected = IsMultiSelected;
        _lastRenderedFocusable = Focusable;
        _lastRenderedAddressable = Addressable;
        _lastRenderedProps = Props;
        _lastRenderedCustomPortsCount = CustomPorts.Count;
        _lastRenderedZIndex = ZIndex;
        _lastRenderedFocusedPortId = FocusedPortId;
        _lastRenderedFocusedCustomPortId = FocusedCustomPortId;
        _lastRenderedAutoPortFocused = AutoPortFocused;
        _lastRenderedProvisionalPort = ProvisionalPort;
        _lastRenderedScale = Scale;
        _lastRenderedIsDropTarget = IsDropTarget;
        _lastRenderedLocked = Locked;
        _lastRenderedHasHitMarker = HasHitMarker;
    }

    private Task HandleFocus() => OnFocus.InvokeAsync();

    // A standard port's own CSS class, plus the highlight class while the keyboard
    // connector-attachment gesture has this exact port as FocusedPortId. The side class is derived
    // from portId itself rather than taken as a separate parameter, so a call site can't hand in a
    // mismatched pair.
    private string StandardPortCssClass(PortId portId) =>
        AutoPortFocused || FocusedPortId == portId
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

    // The visibility gate for the resize spans and the corner handles, suppressed for a
    // multi-selected member, whose shared bounding-box overlay grows its own handles instead.
    private bool ShowSelectionOverlay => IsSelected && !IsMultiSelected && !Locked;

    // Ports show on a single selection and on the drop target of a connector drag, never on hover.
    private bool ShowPorts => ShowSelectionOverlay || (IsDropTarget && !Locked);

    private bool ShowsStandardPort(PortId side) =>
        AutoPortFocused
        || FocusedPortId == side
        || _partition.Any(span =>
            span.Kind == BorderSpanKind.Port && span.CustomPortId is null && span.Side == side
        );

    private bool ShowsCustomPort(Guid portId) =>
        FocusedCustomPortId == portId || _partition.Any(span => span.CustomPortId == portId);

    private static string SideClass(PortId side) =>
        side switch
        {
            PortId.Top => "top",
            PortId.Right => "right",
            PortId.Bottom => "bottom",
            PortId.Left => "left",
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };

    private static string PortPart(BorderSpan span) =>
        span.CustomPortId?.ToString() ?? span.Side.ToString();

    // A span runs along its side from its start edge to its end edge, each a percentage of the
    // side plus a number of port targets divided by scale, so it keeps its screen size between the
    // renders a zoom triggers.
    private static string SpanStyle(BorderSpan span)
    {
        var (offset, length) = span.Side is PortId.Top or PortId.Bottom
            ? ("left", "width")
            : ("top", "height");
        var start = AlongSide(span.Start.Fraction, span.Start.PortTargets);
        var extent = AlongSide(
            span.End.Fraction - span.Start.Fraction,
            span.End.PortTargets - span.Start.PortTargets
        );
        return $"{offset}: {start}; {length}: {extent};";
    }

    private static string AlongSide(double fraction, double portTargets) =>
        $"calc({FormatPercent(fraction)}% + {Format(portTargets)} * var(--d12-port-target) / var(--d12-scale))";

    private static string CustomPortStyle(PortDef port) =>
        PortDotStyle(port.FractionX, port.FractionY);

    private static string PortDotStyle(double fractionX, double fractionY) =>
        $"left: calc({FormatPercent(fractionX)}% - var(--d12-port-dot) / 2); top: calc({FormatPercent(fractionY)}% - var(--d12-port-dot) / 2);";

    private static string FormatPercent(double fraction) => Format(fraction * 100);

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
