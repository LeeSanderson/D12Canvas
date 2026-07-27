namespace D12Canvas.Model;

public sealed class Board
{
    private readonly Dictionary<Guid, ComponentInstance> _components = new();
    private readonly Dictionary<Guid, Group> _groups = new();

    public IReadOnlyCollection<ComponentInstance> Components => _components.Values;
    public IReadOnlyCollection<Group> Groups => _groups.Values;

    public void AddComponent(ComponentInstance instance) => _components.Add(instance.Id, instance);

    public void RemoveComponent(Guid id) => _components.Remove(id);

    public ComponentInstance? GetComponent(Guid id) =>
        _components.TryGetValue(id, out var instance) ? instance : null;

    public void AddGroup(Group group) => _groups.Add(group.Id, group);

    public void RemoveGroup(Guid id) => _groups.Remove(id);

    public Group? GetGroup(Guid id) => _groups.TryGetValue(id, out var group) ? group : null;

    // Ticket 44: a Group's bounds are never stored - only ever resolved on demand from its
    // members, which may themselves be component instances or nested groups. A member id that no
    // longer resolves to anything (deleted out from under the group) is skipped rather than
    // failing the whole computation.
    public Bounds? GetBounds(Group group) =>
        Bounds.Union(
            group.MemberIds.Select(MemberBounds).Where(b => b.HasValue).Select(b => b!.Value)
        );

    private Bounds? MemberBounds(Guid memberId) =>
        GetComponent(memberId)?.Bounds
        ?? (GetGroup(memberId) is { } nested ? GetBounds(nested) : null);

    // Ticket 44: walks up through any nesting to find the outermost group (recursively)
    // containing the given entity id - used so clicking any member of a group, however deeply
    // nested, converges selection on the top-level group (ADR 0006).
    public Group? FindContainingGroup(Guid memberId)
    {
        var parent = _groups.Values.FirstOrDefault(g => g.MemberIds.Contains(memberId));
        if (parent is null)
        {
            return null;
        }

        while (true)
        {
            var grandparent = _groups.Values.FirstOrDefault(g => g.MemberIds.Contains(parent.Id));
            if (grandparent is null)
            {
                return parent;
            }

            parent = grandparent;
        }
    }

    public IReadOnlyCollection<ComponentInstance> GetVisible(Bounds viewport, double overscan = 0)
    {
        var expandedViewport = viewport.ExpandedBy(overscan);

        return _components
            .Values.Where(instance => expandedViewport.Intersects(instance.Bounds))
            .ToList();
    }
}
