using D12Canvas.Model;

namespace D12Canvas;

// A point along one side of an instance, measured from the side's start (its left end for Top and
// Bottom, its top end for Left and Right): a fraction of the side's length plus a distance in port
// targets. The fraction scales with the board and the port targets stay constant on screen, so the
// markup can place a span with the same two numbers at any zoom.
internal readonly record struct SpanEdge(double Fraction, double PortTargets)
{
    public double ScreenAt(double screenLength) =>
        Fraction * screenLength + PortTargets * ScreenPixels.PortTarget;
}

internal enum BorderSpanKind
{
    Port,
    Resize,
}

// One stretch of one side. A port span with no custom port id belongs to the side's standard port.
internal sealed record BorderSpan(
    PortId Side,
    SpanEdge Start,
    SpanEdge End,
    BorderSpanKind Kind,
    Guid? CustomPortId = null
);

// How each side of an instance divides between connecting and resizing, in screen pixels. The run
// is the side less a corner reserve at each end. Every port on the side takes the stretch of the run
// nearest to it, capped at the port target, and two ports closer than that split at their midpoint.
// Resize takes what is left. Anything narrower than the floor is left out. When the split would
// squeeze the standard port below the floor, it keeps its full stretch and the custom ports beside
// it are clipped instead, since auto endpoints and the keyboard pick land on it.
internal static class BorderPartition
{
    private const double StandardFraction = 0.5;

    // A port's stretch reaches half a port target either side of it before anything clips it.
    private const double HalfStretch = 0.5;

    public static IReadOnlyList<BorderSpan> Of(
        double width,
        double height,
        double scale,
        IReadOnlyList<PortDef> customPorts
    ) =>
        StandardPorts
            .All.SelectMany(side =>
                OfSide(
                    side,
                    (side is PortId.Top or PortId.Bottom ? width : height) * scale,
                    customPorts
                        .Where(port => SideOf(port) == side)
                        .Select(port => (port.Id, AlongSide(port, side)))
                        .ToList()
                )
            )
            .ToList();

    public static IReadOnlyList<BorderSpan> OfSide(
        PortId side,
        double screenLength,
        IReadOnlyList<(Guid Id, double Along)> customPorts
    )
    {
        var runStart = new SpanEdge(0, ScreenPixels.CornerReserve / ScreenPixels.PortTarget);
        var runEnd = new SpanEdge(1, -ScreenPixels.CornerReserve / ScreenPixels.PortTarget);
        double At(SpanEdge edge) => edge.ScreenAt(screenLength);
        SpanEdge Later(SpanEdge a, SpanEdge b) => At(a) >= At(b) ? a : b;
        SpanEdge Earlier(SpanEdge a, SpanEdge b) => At(a) <= At(b) ? a : b;
        double Width(SpanEdge start, SpanEdge end) => At(end) - At(start);

        if (Width(runStart, runEnd) < ScreenPixels.AffordanceFloor)
        {
            return [];
        }

        var ports = customPorts
            .Select(port => (Fraction: port.Along, CustomPortId: (Guid?)port.Id))
            .Prepend((Fraction: StandardFraction, CustomPortId: null))
            .OrderBy(port => port.Fraction)
            .ToList();

        var stretches = ports
            .Select(
                (port, i) =>
                {
                    var start = Later(new SpanEdge(port.Fraction, -HalfStretch), runStart);
                    var end = Earlier(new SpanEdge(port.Fraction, HalfStretch), runEnd);
                    if (i > 0)
                    {
                        start = Later(start, Midpoint(ports[i - 1].Fraction, port.Fraction));
                    }

                    if (i < ports.Count - 1)
                    {
                        end = Earlier(end, Midpoint(port.Fraction, ports[i + 1].Fraction));
                    }

                    return (Start: start, End: end);
                }
            )
            .ToList();

        var standard = ports.FindIndex(port => port.CustomPortId is null);
        if (
            Width(stretches[standard].Start, stretches[standard].End) < ScreenPixels.AffordanceFloor
        )
        {
            var full = (
                Start: Later(new SpanEdge(StandardFraction, -HalfStretch), runStart),
                End: Earlier(new SpanEdge(StandardFraction, HalfStretch), runEnd)
            );
            for (var i = 0; i < stretches.Count; i++)
            {
                stretches[i] =
                    i < standard ? (stretches[i].Start, Earlier(stretches[i].End, full.Start))
                    : i > standard ? (Later(stretches[i].Start, full.End), stretches[i].End)
                    : full;
            }
        }

        var spans = new List<BorderSpan>();
        var cursor = runStart;
        for (var i = 0; i < ports.Count; i++)
        {
            var (start, end) = stretches[i];
            if (Width(start, end) < ScreenPixels.AffordanceFloor)
            {
                continue;
            }

            if (Width(cursor, start) >= ScreenPixels.AffordanceFloor)
            {
                spans.Add(new BorderSpan(side, cursor, start, BorderSpanKind.Resize));
            }

            spans.Add(new BorderSpan(side, start, end, BorderSpanKind.Port, ports[i].CustomPortId));
            cursor = end;
        }

        if (Width(cursor, runEnd) >= ScreenPixels.AffordanceFloor)
        {
            spans.Add(new BorderSpan(side, cursor, runEnd, BorderSpanKind.Resize));
        }

        return spans;
    }

    // The side a custom port lies on. Adding a port pins its cross fraction to the side's own 0 or
    // 1, so every port the product creates is on exactly one side, a corner port counting as on
    // its top or bottom side. A port inside the bounds is on none.
    public static PortId? SideOf(PortDef port) =>
        port.FractionY == 0 ? PortId.Top
        : port.FractionY == 1 ? PortId.Bottom
        : port.FractionX == 0 ? PortId.Left
        : port.FractionX == 1 ? PortId.Right
        : null;

    private static double AlongSide(PortDef port, PortId side) =>
        side is PortId.Top or PortId.Bottom ? port.FractionX : port.FractionY;

    private static SpanEdge Midpoint(double first, double second) => new((first + second) / 2, 0);
}
