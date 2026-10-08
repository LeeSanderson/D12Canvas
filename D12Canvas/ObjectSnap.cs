using D12Canvas.Model;

namespace D12Canvas;

internal enum SnapAxis
{
    X,
    Y,
}

internal enum SnapAnchor
{
    Start,
    Centre,
    End,
}

internal readonly record struct AxisSnap(SnapAnchor Anchor, double Target);

internal abstract record SnapGuide;

// A line along the matched coordinate: vertical for the X axis, horizontal for the Y axis.
internal sealed record AlignmentGuide(SnapAxis Axis, double Coordinate) : SnapGuide;

// One gap of an equal-spacing match, measured along the axis from one box to the next and drawn
// at the middle of their perpendicular overlap.
internal sealed record SpacingGuide(SnapAxis Axis, double From, double To, double Cross)
    : SnapGuide;

// The search behind object snapping, run per axis. A move offers the mover's start, centre and end
// against the same three lines of every candidate, plus the equal-spacing positions; a resize
// offers only the edges it moves, which is both edges of a driven axis under a centre resize. A
// snap already held survives until its anchor drifts past the sticky distance, so a match takes
// less effort to keep than to acquire.
internal static class ObjectSnap
{
    public const double StickyFactor = 1.75;

    // Screen pixels per millisecond.
    public const double FastPointerSpeed = 3;

    private const double Coincident = 1e-6;

    private static readonly SnapAnchor[] BoxAnchors =
    [
        SnapAnchor.Start,
        SnapAnchor.Centre,
        SnapAnchor.End,
    ];

    public static AxisSnap? ForMove(
        Bounds mover,
        IReadOnlyList<Bounds> candidates,
        SnapAxis axis,
        double tolerance,
        AxisSnap? held
    )
    {
        var options = PointOptions(candidates, axis, BoxAnchors)
            .Concat(EqualSpacingTargets(mover, candidates, axis))
            .ToList();
        return Resolve(options, anchor => ValueOf(mover, axis, anchor), tolerance, held);
    }

    public static AxisSnap? ForEdges(
        Bounds box,
        IReadOnlyList<SnapAnchor> edges,
        IReadOnlyList<Bounds> candidates,
        SnapAxis axis,
        double tolerance,
        AxisSnap? held
    )
    {
        var options = PointOptions(candidates, axis, edges).ToList();
        return Resolve(options, anchor => ValueOf(box, axis, anchor), tolerance, held);
    }

    public static double Offset(Bounds box, SnapAxis axis, AxisSnap snap) =>
        snap.Target - ValueOf(box, axis, snap.Anchor);

    // The second pass, run from the snapped position, so the guides show where the mover now is
    // rather than where the pointer put it.
    public static IEnumerable<SnapGuide> GuidesForMove(
        Bounds snapped,
        IReadOnlyList<Bounds> candidates,
        SnapAxis axis
    ) =>
        AlignmentGuides(snapped, candidates, axis, BoxAnchors)
            .Concat(SpacingGuides(snapped, candidates, axis));

    public static IEnumerable<SnapGuide> GuidesForEdges(
        Bounds snapped,
        IReadOnlyList<SnapAnchor> edges,
        IReadOnlyList<Bounds> candidates,
        SnapAxis axis
    ) => AlignmentGuides(snapped, candidates, axis, edges);

    public static double ValueOf(Bounds box, SnapAxis axis, SnapAnchor anchor) =>
        anchor switch
        {
            SnapAnchor.Start => Start(box, axis),
            SnapAnchor.Centre => (Start(box, axis) + End(box, axis)) / 2,
            _ => End(box, axis),
        };

    // Where the mover would stand to repeat a gap already present in its row: after or before any
    // row member at that gap, or centred between two members with room for it. The row is filtered
    // by perpendicular overlap before any pair is formed, so the pair enumeration only ever runs
    // over the few entities sharing the mover's row.
    public static IEnumerable<AxisSnap> EqualSpacingTargets(
        Bounds mover,
        IReadOnlyList<Bounds> candidates,
        SnapAxis axis
    )
    {
        var row = Row(mover, candidates, axis);
        var gaps = Gaps(row, axis);
        var length = End(mover, axis) - Start(mover, axis);
        foreach (var gap in gaps)
        {
            foreach (var member in row)
            {
                yield return new AxisSnap(SnapAnchor.Start, End(member, axis) + gap.Size);
                yield return new AxisSnap(SnapAnchor.End, Start(member, axis) - gap.Size);
            }

            if (gap.Size > length)
            {
                yield return new AxisSnap(
                    SnapAnchor.Start,
                    End(gap.Before, axis) + (gap.Size - length) / 2
                );
            }
        }
    }

    public static IReadOnlyList<Bounds> Row(
        Bounds mover,
        IReadOnlyList<Bounds> candidates,
        SnapAxis axis
    ) => candidates.Where(candidate => OverlapsAcross(candidate, mover, axis)).ToList();

    // Each pair of row members facing each other across empty space they share a perpendicular
    // extent over, with no other member reaching into that space.
    public static IReadOnlyList<Gap> Gaps(IReadOnlyList<Bounds> row, SnapAxis axis)
    {
        var gaps = new List<Gap>();
        foreach (var before in row)
        {
            foreach (var after in row)
            {
                var size = Start(after, axis) - End(before, axis);
                if (
                    size > Coincident
                    && OverlapsAcross(before, after, axis)
                    && !row.Any(other =>
                        other != before
                        && other != after
                        && Start(other, axis) < Start(after, axis)
                        && End(other, axis) > End(before, axis)
                    )
                )
                {
                    gaps.Add(new Gap(before, after, size));
                }
            }
        }

        return gaps;
    }

    internal readonly record struct Gap(Bounds Before, Bounds After, double Size);

    private static AxisSnap? Resolve(
        IReadOnlyList<AxisSnap> options,
        Func<SnapAnchor, double> valueOf,
        double tolerance,
        AxisSnap? held
    )
    {
        if (
            held is { } kept
            && options.Contains(kept)
            && Math.Abs(kept.Target - valueOf(kept.Anchor)) <= StickyFactor * tolerance
        )
        {
            return kept;
        }

        AxisSnap? nearest = null;
        var nearestDistance = tolerance;
        foreach (var option in options)
        {
            var distance = Math.Abs(option.Target - valueOf(option.Anchor));
            if (distance <= nearestDistance && (nearest is null || distance < nearestDistance))
            {
                nearest = option;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static IEnumerable<AxisSnap> PointOptions(
        IReadOnlyList<Bounds> candidates,
        SnapAxis axis,
        IReadOnlyList<SnapAnchor> moverAnchors
    ) =>
        candidates
            .SelectMany(candidate => BoxAnchors.Select(anchor => ValueOf(candidate, axis, anchor)))
            .Distinct()
            .SelectMany(line => moverAnchors.Select(anchor => new AxisSnap(anchor, line)));

    private static IEnumerable<SnapGuide> AlignmentGuides(
        Bounds snapped,
        IReadOnlyList<Bounds> candidates,
        SnapAxis axis,
        IReadOnlyList<SnapAnchor> moverAnchors
    )
    {
        var lines = candidates
            .SelectMany(candidate => BoxAnchors.Select(anchor => ValueOf(candidate, axis, anchor)))
            .ToList();
        return moverAnchors
            .Select(anchor => ValueOf(snapped, axis, anchor))
            .Where(value => lines.Any(line => Math.Abs(line - value) < Coincident))
            .Distinct()
            .Select(value => new AlignmentGuide(axis, value));
    }

    // Every gap beside the mover that repeats another, drawn together with the gaps it repeats.
    // A gap the mover now stands inside is no longer a gap of the row.
    private static IEnumerable<SnapGuide> SpacingGuides(
        Bounds snapped,
        IReadOnlyList<Bounds> candidates,
        SnapAxis axis
    )
    {
        var row = Row(snapped, candidates, axis);
        var rowGaps = Gaps(row, axis)
            .Where(gap =>
                !(
                    Start(snapped, axis) < Start(gap.After, axis)
                    && End(snapped, axis) > End(gap.Before, axis)
                )
            );
        var moverGaps = new List<Gap>();
        if (
            row.Where(member => End(member, axis) <= Start(snapped, axis) + Coincident)
                .OrderByDescending(member => End(member, axis))
                .Select(member => (Bounds?)member)
                .FirstOrDefault() is
            { } before
        )
        {
            moverGaps.Add(new Gap(before, snapped, Start(snapped, axis) - End(before, axis)));
        }

        if (
            row.Where(member => Start(member, axis) >= End(snapped, axis) - Coincident)
                .OrderBy(member => Start(member, axis))
                .Select(member => (Bounds?)member)
                .FirstOrDefault() is
            { } after
        )
        {
            moverGaps.Add(new Gap(snapped, after, Start(after, axis) - End(snapped, axis)));
        }

        var allGaps = rowGaps.Concat(moverGaps).ToList();
        return moverGaps
            .Where(gap => gap.Size > Coincident)
            .SelectMany(gap =>
            {
                var repeats = allGaps
                    .Where(other => other != gap && Math.Abs(other.Size - gap.Size) < Coincident)
                    .ToList();
                return repeats.Count == 0 ? Enumerable.Empty<Gap>() : repeats.Prepend(gap);
            })
            .Distinct()
            .Select(gap => SpacingGuideFor(gap, axis));
    }

    private static SnapGuide SpacingGuideFor(Gap gap, SnapAxis axis)
    {
        var across = Across(axis);
        var overlapStart = Math.Max(Start(gap.Before, across), Start(gap.After, across));
        var overlapEnd = Math.Min(End(gap.Before, across), End(gap.After, across));
        return new SpacingGuide(
            axis,
            End(gap.Before, axis),
            Start(gap.After, axis),
            (overlapStart + overlapEnd) / 2
        );
    }

    private static bool OverlapsAcross(Bounds first, Bounds second, SnapAxis axis)
    {
        var across = Across(axis);
        return Start(first, across) < End(second, across)
            && Start(second, across) < End(first, across);
    }

    private static SnapAxis Across(SnapAxis axis) => axis == SnapAxis.X ? SnapAxis.Y : SnapAxis.X;

    private static double Start(Bounds box, SnapAxis axis) => axis == SnapAxis.X ? box.X : box.Y;

    private static double End(Bounds box, SnapAxis axis) =>
        axis == SnapAxis.X ? box.Right : box.Bottom;
}
