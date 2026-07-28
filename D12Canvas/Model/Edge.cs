namespace D12Canvas.Model;

// ADR 0005: a Board entity connecting two endpoints, straight-line routing only for now (per-edge
// routing/arrows are ticket 52's concern). Each endpoint is either attached to a port or floating
// at a fixed point (ticket 49) - Source/Target can independently be either shape, and can change
// shape after creation (an attached endpoint dragged off its port becomes floating, and vice versa).
public sealed class Edge
{
    public Guid Id { get; }
    public IEdgeEndpoint Source { get; set; }
    public IEdgeEndpoint Target { get; set; }

    public Edge(IEdgeEndpoint source, IEdgeEndpoint target, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Source = source;
        Target = target;
    }
}
