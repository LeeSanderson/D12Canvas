namespace D12Canvas.Model;

public sealed class Board
{
    private readonly Dictionary<Guid, ComponentInstance> _components = new();

    public IReadOnlyCollection<ComponentInstance> Components => _components.Values;

    public void AddComponent(ComponentInstance instance) => _components.Add(instance.Id, instance);

    public void RemoveComponent(Guid id) => _components.Remove(id);

    public ComponentInstance? GetComponent(Guid id) =>
        _components.TryGetValue(id, out var instance) ? instance : null;

    public IReadOnlyCollection<ComponentInstance> GetVisible(Bounds viewport, double overscan = 0)
    {
        var expandedViewport = viewport.ExpandedBy(overscan);

        return _components
            .Values.Where(instance => expandedViewport.Intersects(instance.Bounds))
            .ToList();
    }
}
