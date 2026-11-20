namespace D12Canvas.Model;

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
