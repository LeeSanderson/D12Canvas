namespace D12Canvas.Model;

// Ticket 55: identifies which port on an instance a connector-drag gesture started from - one of
// the four automatic standard ports, or one of the instance's own runtime-added custom ports (ADR
// 0005). A closed union (rather than two separate StartPortDrag overloads propagating all the way
// through DiagramCanvas) so the rest of the gesture doesn't need its own branch until the moment it
// builds the actual IEdgeEndpoint, via ToEndpoint.
public readonly struct PortRef
{
    private readonly PortId? _standardPortId;
    private readonly Guid? _customPortId;

    private PortRef(PortId? standardPortId, Guid? customPortId)
    {
        _standardPortId = standardPortId;
        _customPortId = customPortId;
    }

    public static PortRef Standard(PortId portId) => new(portId, null);

    public static PortRef Custom(Guid portId) => new(null, portId);

    public IEdgeEndpoint ToEndpoint(Guid componentId) =>
        _standardPortId is { } standard
            ? new PortEndpoint(componentId, standard)
            : new CustomPortEndpoint(componentId, _customPortId!.Value);
}
