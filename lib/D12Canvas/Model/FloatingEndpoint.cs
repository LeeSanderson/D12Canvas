namespace D12Canvas.Model;

public readonly record struct FloatingEndpoint(double X, double Y) : IEdgeEndpoint
{
    Guid? IEdgeEndpoint.ComponentId => null;
}
