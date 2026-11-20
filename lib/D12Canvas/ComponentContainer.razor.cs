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

    [Parameter]
    public Guid? EntityId { get; set; }

    [Parameter]
    public bool Locked { get; set; }

    [Parameter]
    public bool HasHitMarker { get; set; } = true;

    [Parameter]
    public string? AccessibleName { get; set; }

    [Parameter]
    public string? Role { get; set; }

    [Parameter]
    public bool IsSelected { get; set; }

    [Parameter]
    public bool Focusable { get; set; } = true;

    [Parameter]
    public bool Addressable { get; set; } = true;

    [Parameter]
    public object? Props { get; set; }

    [Parameter]
    public bool IsMultiSelected { get; set; }

    [Parameter]
    public EventCallback OnFocus { get; set; }

    [Parameter]
    public IReadOnlyList<PortDef> CustomPorts { get; set; } = Array.Empty<PortDef>();

    [Parameter]
    public PortId? FocusedPortId { get; set; }

    [Parameter]
    public Guid? FocusedCustomPortId { get; set; }

    [Parameter]
    public bool AutoPortFocused { get; set; }

    [Parameter]
    public (double FractionX, double FractionY)? ProvisionalPort { get; set; }

    [Parameter]
    public double Scale { get; set; } = 1;

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
        return X != _lastRenderedX
            || Y != _lastRenderedY
            || Width != _lastRenderedWidth
            || Height != _lastRenderedHeight
            || IsSelected != _lastRenderedIsSelected
            || IsMultiSelected != _lastRenderedIsMultiSelected
            || Focusable != _lastRenderedFocusable
            || Addressable != _lastRenderedAddressable
            || !Equals(Props, _lastRenderedProps)
            || CustomPorts.Count != _lastRenderedCustomPortsCount
            || ZIndex != _lastRenderedZIndex
            || FocusedPortId != _lastRenderedFocusedPortId
            || FocusedCustomPortId != _lastRenderedFocusedCustomPortId
            || AutoPortFocused != _lastRenderedAutoPortFocused
            || ProvisionalPort != _lastRenderedProvisionalPort
            || IsDropTarget != _lastRenderedIsDropTarget
            || Locked != _lastRenderedLocked
            || HasHitMarker != _lastRenderedHasHitMarker
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

    private string CustomPortCssClass(Guid portId) =>
        FocusedCustomPortId == portId ? "port custom-port port-focused" : "port custom-port";

    private bool ShowSelectionOverlay => IsSelected && !IsMultiSelected && !Locked;

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
