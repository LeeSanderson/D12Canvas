namespace D12Canvas.Model;

// ADR 0006/0007: promotes an ad-hoc multi-selection into a persistent board entity. MemberIds is
// a reference list held by the group, not a back-pointer on members (CONTEXT.md) - membership can
// include component instance ids and/or nested Group ids, and grouping/ungrouping never mutates
// the member entities themselves. Bounds are deliberately not stored here - Board.GetBounds(group)
// resolves them from members on demand instead.
public sealed class Group
{
    public Guid Id { get; }
    public IReadOnlyList<Guid> MemberIds { get; }

    public Group(IReadOnlyList<Guid> memberIds, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        MemberIds = memberIds;
    }
}
