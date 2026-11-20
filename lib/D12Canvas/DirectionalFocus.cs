using D12Canvas.Model;

namespace D12Canvas;

internal enum FocusDirection
{
    Left,
    Right,
    Up,
    Down,
}

internal readonly record struct FocusCandidate(Guid Id, Bounds Bounds);

internal static class DirectionalFocus
{
    public static FocusDirection? DirectionFor(string code) =>
        code switch
        {
            "ArrowLeft" => FocusDirection.Left,
            "ArrowRight" => FocusDirection.Right,
            "ArrowUp" => FocusDirection.Up,
            "ArrowDown" => FocusDirection.Down,
            _ => null,
        };

    public static Guid? Nearest(
        Bounds origin,
        FocusDirection direction,
        IEnumerable<FocusCandidate> candidates
    )
    {
        var reaches = candidates
            .Select(candidate => ReachOf(origin, direction, candidate))
            .Where(reach => reach is not null)
            .Select(reach => reach!.Value)
            .ToList();

        var inLine = reaches.Where(reach => reach.InRowOrColumn).ToList();
        var ranked =
            inLine.Count > 0
                ? inLine.OrderBy(reach => reach.Gap)
                : reaches.OrderBy(reach => reach.Gap + reach.Offset);

        return ranked
            .ThenBy(reach => reach.Candidate.Bounds.Y)
            .ThenBy(reach => reach.Candidate.Bounds.X)
            .Select(reach => (Guid?)reach.Candidate.Id)
            .FirstOrDefault();
    }

    private readonly record struct Reach(
        FocusCandidate Candidate,
        double Gap,
        double Offset,
        bool InRowOrColumn
    );

    private static Reach? ReachOf(Bounds origin, FocusDirection direction, FocusCandidate candidate)
    {
        var box = candidate.Bounds;
        var gap = direction switch
        {
            FocusDirection.Right => box.X - origin.Right,
            FocusDirection.Left => origin.X - box.Right,
            FocusDirection.Down => box.Y - origin.Bottom,
            _ => origin.Y - box.Bottom,
        };
        if (gap < 0)
        {
            return null;
        }

        var horizontal = direction is FocusDirection.Left or FocusDirection.Right;
        var (originMin, originMax) = horizontal
            ? (origin.Y, origin.Bottom)
            : (origin.X, origin.Right);
        var (boxMin, boxMax) = horizontal ? (box.Y, box.Bottom) : (box.X, box.Right);

        var separation = Math.Max(originMin, boxMin) - Math.Min(originMax, boxMax);
        var inRowOrColumn =
            separation < 0
            || (originMin == originMax && boxMin <= originMin && originMin <= boxMax);

        return new Reach(candidate, gap, Math.Max(0, separation), inRowOrColumn);
    }
}
