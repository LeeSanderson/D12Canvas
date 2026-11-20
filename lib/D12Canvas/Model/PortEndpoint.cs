namespace D12Canvas.Model;

public readonly record struct PortEndpoint(Guid ComponentId, PortId PortId) : IEdgeEndpoint
{
    Guid? IEdgeEndpoint.ComponentId => ComponentId;
}
