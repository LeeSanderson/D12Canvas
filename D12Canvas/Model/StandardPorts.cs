namespace D12Canvas.Model;

// Fractional border-center positions for the four automatic standard ports, expressed as a
// fraction of Bounds rather than an absolute offset - a port's live position is always derived
// from an instance's current Bounds, so it tracks move/resize for free.
public static class StandardPorts
{
    public static readonly IReadOnlyList<PortId> All = new[]
    {
        PortId.Top,
        PortId.Right,
        PortId.Bottom,
        PortId.Left,
    };

    public static (double FractionX, double FractionY) FractionOf(PortId portId) =>
        portId switch
        {
            PortId.Top => (0.5, 0),
            PortId.Right => (1, 0.5),
            PortId.Bottom => (0.5, 1),
            PortId.Left => (0, 0.5),
            _ => throw new ArgumentOutOfRangeException(nameof(portId)),
        };
}
