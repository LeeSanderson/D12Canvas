using D12Canvas.Model;

namespace D12Canvas;

// Along runs from a side's top or left end at 0 to its bottom or right end at 1, so the two
// opposite sides share it and crossing between them keeps the position.
internal readonly record struct ProvisionalPort(PortId Side, double Along)
{
    public (double X, double Y) Fractions =>
        Side switch
        {
            PortId.Top => (Along, 0),
            PortId.Bottom => (Along, 1),
            PortId.Left => (0, Along),
            PortId.Right => (1, Along),
            _ => throw new ArgumentOutOfRangeException(nameof(Side), Side, null),
        };
}

// Step takes a board coordinate along the side and a direction of -1 or 1 and gives the
// coordinate one step on, before the side clamps it, so a step that overshoots stops at the
// corner.
internal static class PortPlacement
{
    public const double StartAlong = 0.25;

    public static ProvisionalPort Start(Bounds bounds, double? gridSpacing)
    {
        var start = bounds.X + StartAlong * bounds.Width;
        var snapped = gridSpacing is { } spacing ? GridSnap.NearestLine(start, spacing) : start;
        return new ProvisionalPort(PortId.Top, AlongAt(snapped, bounds.X, bounds.Width));
    }

    public static ProvisionalPort Move(
        ProvisionalPort port,
        Bounds bounds,
        int directionX,
        int directionY,
        Func<double, int, double> step
    )
    {
        var runsAcross = port.Side is PortId.Top or PortId.Bottom;
        var along = runsAcross ? directionX : directionY;
        var cross = runsAcross ? directionY : directionX;

        if (along != 0)
        {
            return Slide(port, bounds, along, step) ?? TurnAtCorner(port, along);
        }

        if (cross == 0)
        {
            return port;
        }

        if (
            AdjacentAtCorner(port) is { } adjacent
            && Slide(adjacent, bounds, cross, step) is { } turned
        )
        {
            return turned;
        }

        return cross == InwardDirection(port.Side)
            ? port with
            {
                Side = Opposite(port.Side),
            }
            : port;
    }

    private static ProvisionalPort? Slide(
        ProvisionalPort port,
        Bounds bounds,
        int direction,
        Func<double, int, double> step
    )
    {
        var (origin, length) = port.Side is PortId.Top or PortId.Bottom
            ? (bounds.X, bounds.Width)
            : (bounds.Y, bounds.Height);
        var blocked = length <= 0 || (direction < 0 ? port.Along <= 0 : port.Along >= 1);
        if (blocked)
        {
            return null;
        }

        var stepped = step(origin + port.Along * length, direction);
        return port with { Along = AlongAt(stepped, origin, length) };
    }

    public static double AlongAt(double coordinate, double origin, double length) =>
        coordinate <= origin ? 0
        : coordinate >= origin + length ? 1
        : (coordinate - origin) / length;

    private static ProvisionalPort TurnAtCorner(ProvisionalPort port, int direction) =>
        new(SideMeetingAt(port.Side, atStart: direction < 0), EndNearest(port.Side));

    private static ProvisionalPort? AdjacentAtCorner(ProvisionalPort port)
    {
        if (port.Along is not (0 or 1))
        {
            return null;
        }

        return new ProvisionalPort(
            SideMeetingAt(port.Side, atStart: port.Along == 0),
            EndNearest(port.Side)
        );
    }

    private static PortId SideMeetingAt(PortId side, bool atStart) =>
        side is PortId.Top or PortId.Bottom
            ? (atStart ? PortId.Left : PortId.Right)
            : (atStart ? PortId.Top : PortId.Bottom);

    private static double EndNearest(PortId side) => side is PortId.Top or PortId.Left ? 0 : 1;

    private static int InwardDirection(PortId side) => side is PortId.Top or PortId.Left ? 1 : -1;

    private static PortId Opposite(PortId side) =>
        side switch
        {
            PortId.Top => PortId.Bottom,
            PortId.Bottom => PortId.Top,
            PortId.Left => PortId.Right,
            PortId.Right => PortId.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
        };
}
