using D12Canvas.Model;

namespace D12Canvas;

internal readonly record struct ArrangedEntity(Guid Id, Bounds Bounds);

internal readonly record struct ArrangeDelta(Guid Id, double Dx, double Dy);

internal enum AlignEdge
{
    Left,
    Centre,
    Right,
    Top,
    Middle,
    Bottom,
}

internal enum DistributeAxis
{
    Horizontal,
    Vertical,
}

// Both computations work on whole entities, so a group is handed in as one box and comes back as
// one delta for its members to share. A grid spacing of null means snap is off. An entity left
// where it is gets no delta, so a satisfied arrangement produces nothing to record.
internal static class AlignDistribute
{
    public const int AlignThreshold = 2;
    public const int DistributeThreshold = 3;

    private const double Tolerance = 1e-9;

    // The target is the selection box's own edge or centre line, snapped as a single coordinate,
    // and every entity's matching line is then set to exactly that value. Snapping each result
    // instead would pull centre and right alignment apart wherever widths differ.
    public static IReadOnlyList<ArrangeDelta> Align(
        IReadOnlyList<ArrangedEntity> entities,
        AlignEdge edge,
        double? gridSpacing
    )
    {
        if (
            entities.Count < AlignThreshold
            || Bounds.Union(entities.Select(entity => entity.Bounds)) is not { } box
        )
        {
            return [];
        }

        var horizontal = edge is AlignEdge.Left or AlignEdge.Centre or AlignEdge.Right;
        var target = Snap(LineOf(box, edge), gridSpacing);
        return WithoutStayers(
            entities.Select(entity =>
                Shift(entity.Id, target - LineOf(entity.Bounds, edge), horizontal)
            )
        );
    }

    // Entities are ordered by their centre on the axis and the whitespace between neighbours made
    // equal. The first entity never moves. With snap off the last stays too; with it on the gap is
    // a whole number of grid steps, never zero when the ideal gap is positive, so the last drifts.
    public static IReadOnlyList<ArrangeDelta> Distribute(
        IReadOnlyList<ArrangedEntity> entities,
        DistributeAxis axis,
        double? gridSpacing
    )
    {
        if (entities.Count < DistributeThreshold)
        {
            return [];
        }

        var horizontal = axis == DistributeAxis.Horizontal;
        double Start(Bounds bounds) => horizontal ? bounds.X : bounds.Y;
        double Size(Bounds bounds) => horizontal ? bounds.Width : bounds.Height;

        var ordered = entities
            .OrderBy(entity => Start(entity.Bounds) + Size(entity.Bounds) / 2)
            .ToList();
        var first = ordered[0].Bounds;
        var last = ordered[^1].Bounds;
        var span = Start(last) + Size(last) - Start(first);
        var idealGap = (span - ordered.Sum(entity => Size(entity.Bounds))) / (ordered.Count - 1);
        var gap = SnapGap(idealGap, gridSpacing);

        var deltas = new List<ArrangeDelta>();
        var next = Start(first);
        foreach (var entity in ordered)
        {
            deltas.Add(Shift(entity.Id, next - Start(entity.Bounds), horizontal));
            next += Size(entity.Bounds) + gap;
        }

        return WithoutStayers(deltas);
    }

    private static double LineOf(Bounds bounds, AlignEdge edge) =>
        edge switch
        {
            AlignEdge.Left => bounds.X,
            AlignEdge.Centre => bounds.X + bounds.Width / 2,
            AlignEdge.Right => bounds.Right,
            AlignEdge.Top => bounds.Y,
            AlignEdge.Middle => bounds.Y + bounds.Height / 2,
            AlignEdge.Bottom => bounds.Bottom,
            _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, null),
        };

    private static double Snap(double coordinate, double? gridSpacing) =>
        gridSpacing is { } spacing ? GridSnap.NearestLine(coordinate, spacing) : coordinate;

    // Without the clamp a row of small gaps would round to zero and distribute would pack.
    private static double SnapGap(double idealGap, double? gridSpacing)
    {
        if (gridSpacing is not { } spacing)
        {
            return idealGap;
        }

        var rounded = GridSnap.NearestLine(idealGap, spacing);
        return idealGap > 0 ? Math.Max(rounded, spacing) : rounded;
    }

    private static ArrangeDelta Shift(Guid id, double shift, bool horizontal) =>
        horizontal ? new ArrangeDelta(id, shift, 0) : new ArrangeDelta(id, 0, shift);

    private static IReadOnlyList<ArrangeDelta> WithoutStayers(IEnumerable<ArrangeDelta> deltas) =>
        deltas
            .Select(delta => new ArrangeDelta(
                delta.Id,
                ZeroIfNegligible(delta.Dx),
                ZeroIfNegligible(delta.Dy)
            ))
            .Where(delta => delta.Dx != 0 || delta.Dy != 0)
            .ToList();

    private static double ZeroIfNegligible(double shift) => Math.Abs(shift) < Tolerance ? 0 : shift;
}
