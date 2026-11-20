namespace D12Canvas.Pointer;

internal sealed class SelectionSnapshot
{
    public SelectionSnapshot(IEnumerable<Guid> instanceIds, IEnumerable<Guid> edgeIds)
    {
        InstanceIds = instanceIds.ToHashSet();
        EdgeIds = edgeIds.ToHashSet();
    }

    public IReadOnlySet<Guid> InstanceIds { get; }
    public IReadOnlySet<Guid> EdgeIds { get; }
}
