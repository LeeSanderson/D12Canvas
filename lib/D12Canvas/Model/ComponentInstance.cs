namespace D12Canvas.Model;

public sealed class ComponentInstance
{
    public Guid Id { get; }
    public string ComponentTypeKey { get; }
    public object Props { get; set; }
    public Bounds Bounds { get; set; }
    public int ZIndex { get; set; }

    // No command changes a locked instance and no primary press or marquee reaches it, while the
    // keyboard still does. Written only through ChangeLockedCommand.
    public bool Locked { get; set; }

    // An end user's own runtime-added ports on this specific instance - nothing a component
    // type's developer declares at registration. A plain mutable list (rather than a dedicated
    // Add/Remove method) since AddCustomPortCommand and RemoveCustomPortCommand own the undo/redo
    // discipline around mutating it.
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
