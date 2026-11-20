using D12Canvas.Model;

namespace D12Canvas;

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

internal sealed record BorderSpan(
    PortId Side,
    SpanEdge Start,
    SpanEdge End,
    BorderSpanKind Kind,
    Guid? CustomPortId = null
);

internal static class BorderPartition
{
    private const double StandardFraction = 0.5;

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

    public static PortId? SideOf(PortDef port) => SideOf(port.FractionX, port.FractionY);

    public static PortId? SideOf(double fractionX, double fractionY) =>
        fractionY <= 0 ? PortId.Top
        : fractionY >= 1 ? PortId.Bottom
        : fractionX <= 0 ? PortId.Left
        : fractionX >= 1 ? PortId.Right
        : null;

    private static double AlongSide(PortDef port, PortId side) =>
        side is PortId.Top or PortId.Bottom ? port.FractionX : port.FractionY;

    private static SpanEdge Midpoint(double first, double second) => new((first + second) / 2, 0);
}
