namespace D12Canvas.Model;

public sealed class ComponentInstance
{
    public Guid Id { get; }
    public string ComponentTypeKey { get; }
    public object Props { get; set; }
    public Bounds Bounds { get; set; }
    public int ZIndex { get; set; }

    public ComponentInstance(
        string componentTypeKey,
        object props,
        Bounds bounds,
        int zIndex = 0,
        Guid? id = null
    )
    {
        Id = id ?? Guid.NewGuid();
        ComponentTypeKey = componentTypeKey;
        Props = props;
        Bounds = bounds;
        ZIndex = zIndex;
    }
}
