namespace D12Canvas.Pointer;

// Both selection sets as they were when a press landed, before any press-time collapse.
// Cancel restores it; a Shift marquee unions into it.
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
