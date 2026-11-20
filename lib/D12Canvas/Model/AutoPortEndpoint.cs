namespace D12Canvas.Model;

public readonly record struct AutoPortEndpoint(Guid ComponentId) : IEdgeEndpoint
{
    Guid? IEdgeEndpoint.ComponentId => ComponentId;
}
