namespace D12Canvas.Model;

public sealed class ComponentInstance
{
    public Guid Id { get; } = Guid.NewGuid();
    public string ComponentTypeKey { get; }
    public object Props { get; set; }
    public Bounds Bounds { get; set; }
    public int ZIndex { get; set; }

    public ComponentInstance(string componentTypeKey, object props, Bounds bounds, int zIndex = 0)
    {
        ComponentTypeKey = componentTypeKey;
        Props = props;
        Bounds = bounds;
        ZIndex = zIndex;
    }
}
