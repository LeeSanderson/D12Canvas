using D12Canvas.Model;

namespace D12Canvas;

// Each step moves the box a whole gap further from the source, away from every obstacle it has
// passed, so the search ends once the box is clear of them all.
internal static class QuickCreateSlot
{
    public static Bounds For(
        Bounds source,
        PortId side,
        double gap,
        double? gridSpacing,
        IReadOnlyCollection<Bounds> obstacles
    )
    {
        if (gap <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gap), gap, "The gap must be positive.");
        }

        for (var steps = 1; ; steps++)
        {
            var candidate = Snapped(Candidate(source, side, steps * gap), gridSpacing);
            if (!obstacles.Any(obstacle => obstacle.Intersects(candidate)))
            {
                return candidate;
            }
        }
    }

    private static Bounds Candidate(Bounds source, PortId side, double distance) =>
        side switch
        {
            PortId.Right => source with { X = source.Right + distance },
            PortId.Left => source with { X = source.X - distance - source.Width },
            PortId.Bottom => source with { Y = source.Bottom + distance },
            PortId.Top => source with { Y = source.Y - distance - source.Height },
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
        };

    private static Bounds Snapped(Bounds bounds, double? gridSpacing) =>
        gridSpacing is { } spacing
            ? bounds with
            {
                X = GridSnap.NearestLine(bounds.X, spacing),
                Y = GridSnap.NearestLine(bounds.Y, spacing),
            }
            : bounds;
}
