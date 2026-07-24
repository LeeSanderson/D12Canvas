namespace D12Canvas.Model;

public sealed class Board
{
    private readonly Dictionary<Guid, ComponentInstance> _components = new();

    public IReadOnlyCollection<ComponentInstance> Components => _components.Values;

    public void AddComponent(ComponentInstance instance) => _components.Add(instance.Id, instance);

    public void RemoveComponent(Guid id) => _components.Remove(id);

    public ComponentInstance? GetComponent(Guid id) =>
        _components.TryGetValue(id, out var instance) ? instance : null;
}
