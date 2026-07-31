namespace D12Canvas.Model;

public sealed class ComponentInstance
{
    public Guid Id { get; }
    public string ComponentTypeKey { get; }
    public object Props { get; set; }
    public Bounds Bounds { get; set; }
    public int ZIndex { get; set; }

    // An end user's own runtime-added ports on this specific instance - nothing a component
    // type's developer declares at registration. A plain mutable list (rather than a dedicated
    // Add/Remove method) since AddCustomPortCommand already owns the undo/redo discipline around
    // mutating it.
    public List<PortDef> CustomPorts { get; }

    public ComponentInstance(
        string componentTypeKey,
        object props,
        Bounds bounds,
        int zIndex = 0,
        Guid? id = null,
        IReadOnlyList<PortDef>? customPorts = null
    )
    {
        Id = id ?? Guid.NewGuid();
        ComponentTypeKey = componentTypeKey;
        Props = props;
        Bounds = bounds;
        ZIndex = zIndex;
        CustomPorts = customPorts is null ? new List<PortDef>() : new List<PortDef>(customPorts);
    }
}
