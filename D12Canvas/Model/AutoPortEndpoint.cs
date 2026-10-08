namespace D12Canvas.Model;

// An edge endpoint attached to a component instance without naming a port. It resolves, together
// with the edge's other end, to the standard port on the side facing that end (AutoPortSide), so
// the side is chosen again whenever either end moves.
public readonly record struct AutoPortEndpoint(Guid ComponentId) : IEdgeEndpoint
{
    Guid? IEdgeEndpoint.ComponentId => ComponentId;
}
