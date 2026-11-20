namespace D12Canvas.Model;

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
