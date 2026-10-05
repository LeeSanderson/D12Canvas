namespace D12Canvas.Pointer;

// Both selection fields as they were when a press landed, before any press-time collapse.
// Cancel restores it; a Shift marquee unions into it.
internal sealed class SelectionSnapshot
{
    public SelectionSnapshot(IEnumerable<Guid> instanceIds, Guid? edgeId)
    {
        InstanceIds = instanceIds.ToHashSet();
        EdgeId = edgeId;
    }

    public IReadOnlySet<Guid> InstanceIds { get; }
    public Guid? EdgeId { get; }
}
