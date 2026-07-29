namespace D12Canvas.Model;

// ADR 0005: a Board entity connecting two endpoints. Each endpoint is either attached to a port or
// floating at a fixed point (ticket 49) - Source/Target can independently be either shape, and can
// change shape after creation (an attached endpoint dragged off its port becomes floating, and vice
// versa). Routing style and arrowheads (ticket 52) are per-edge, never board-wide, and default to
// the single-directed-arrow case ADR 0005 front-loads (Straight, no arrow at Source, arrow at Target).
public sealed class Edge
{
    public Guid Id { get; }
    public IEdgeEndpoint Source { get; set; }
    public IEdgeEndpoint Target { get; set; }
    public EdgeRouting RoutingStyle { get; set; }
    public ArrowStyle SourceArrow { get; set; }
    public ArrowStyle TargetArrow { get; set; }

    public Edge(
        IEdgeEndpoint source,
        IEdgeEndpoint target,
        Guid? id = null,
        EdgeRouting routingStyle = EdgeRouting.Straight,
        ArrowStyle sourceArrow = ArrowStyle.None,
        ArrowStyle targetArrow = ArrowStyle.Arrow
    )
    {
        Id = id ?? Guid.NewGuid();
        Source = source;
        Target = target;
        RoutingStyle = routingStyle;
        SourceArrow = sourceArrow;
        TargetArrow = targetArrow;
    }
}
