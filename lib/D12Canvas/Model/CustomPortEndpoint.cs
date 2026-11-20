namespace D12Canvas.Model;

public readonly record struct CustomPortEndpoint(Guid ComponentId, Guid PortId) : IEdgeEndpoint
{
    Guid? IEdgeEndpoint.ComponentId => ComponentId;
}
