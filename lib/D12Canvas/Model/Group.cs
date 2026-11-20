namespace D12Canvas.Model;

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
