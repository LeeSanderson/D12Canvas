namespace D12Canvas;

internal sealed class DuplicateRun
{
    public const double FirstStep = PasteCascade.Step;

    private LastDuplicate? _last;

    private sealed record LastDuplicate(
        IReadOnlySet<Guid> InstanceIds,
        IReadOnlySet<Guid> EdgeIds,
        (double X, double Y) SourceTopLeft
    );

    public (double Dx, double Dy) OffsetFor(
        IReadOnlySet<Guid> selectedInstanceIds,
        IReadOnlySet<Guid> selectedEdgeIds,
        (double X, double Y) selectionTopLeft
    ) =>
        _last is { } last && IsSelected(last, selectedInstanceIds, selectedEdgeIds)
            ? (selectionTopLeft.X - last.SourceTopLeft.X, selectionTopLeft.Y - last.SourceTopLeft.Y)
            : (FirstStep, FirstStep);

    public void RecordDuplicate(
        IEnumerable<Guid> instanceIds,
        IEnumerable<Guid> edgeIds,
        (double X, double Y) sourceTopLeft
    ) => _last = new LastDuplicate(instanceIds.ToHashSet(), edgeIds.ToHashSet(), sourceTopLeft);

    public void SelectionChanged(
        IReadOnlySet<Guid> selectedInstanceIds,
        IReadOnlySet<Guid> selectedEdgeIds
    )
    {
        if (_last is { } last && !IsSelected(last, selectedInstanceIds, selectedEdgeIds))
        {
            _last = null;
        }
    }

    public void BoardChanged(Func<Guid, bool> exists)
    {
        if (_last is { } last && !last.InstanceIds.Concat(last.EdgeIds).All(exists))
        {
            _last = null;
        }
    }

    public void End() => _last = null;

    private static bool IsSelected(
        LastDuplicate last,
        IReadOnlySet<Guid> selectedInstanceIds,
        IReadOnlySet<Guid> selectedEdgeIds
    ) => last.InstanceIds.SetEquals(selectedInstanceIds) && last.EdgeIds.SetEquals(selectedEdgeIds);
}
