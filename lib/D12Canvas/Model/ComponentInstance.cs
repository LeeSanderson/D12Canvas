namespace D12Canvas.Model;

public sealed class ComponentInstance
{
    public Guid Id { get; }
    public string ComponentTypeKey { get; }
    public object Props { get; set; }
    public Bounds Bounds { get; set; }
    public int ZIndex { get; set; }

    public bool Locked { get; set; }

    public List<PortDef> CustomPorts { get; }

    public ComponentInstance(
        string componentTypeKey,
        object props,
        Bounds bounds,
        int zIndex = 0,
        Guid? id = null,
        IReadOnlyList<PortDef>? customPorts = null,
        bool locked = false
    )
    {
        Id = id ?? Guid.NewGuid();
        ComponentTypeKey = componentTypeKey;
        Props = props;
        Bounds = bounds;
        ZIndex = zIndex;
        CustomPorts = customPorts is null ? new List<PortDef>() : new List<PortDef>(customPorts);
        Locked = locked;
    }
}
