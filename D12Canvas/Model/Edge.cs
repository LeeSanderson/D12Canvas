namespace D12Canvas.Model;

// A Board entity connecting two endpoints. Each endpoint is either attached to a port or floating
// at a fixed point - Source/Target can independently be either shape, and can change shape after
// creation (an attached endpoint dragged off its port becomes floating, and vice versa). Routing
// style and arrowheads are per-edge, never board-wide, and default to the single-directed-arrow
// case (Straight, no arrow at Source, arrow at Target). Label is a full ComponentInstance embedded
// directly here (not a Board.Components entry) - it has no existence independent of the edge that
// owns it, so deleting the edge removes the label by construction, with no separate cleanup needed.
// Color is a CSS colour or null for no author opinion; an empty string is stored as null so
// absence has one representation.
public sealed class Edge
{
    private string? _color;

    public Guid Id { get; }
    public IEdgeEndpoint Source { get; set; }
    public IEdgeEndpoint Target { get; set; }
    public EdgeRouting RoutingStyle { get; set; }
    public ArrowStyle SourceArrow { get; set; }
    public ArrowStyle TargetArrow { get; set; }
    public ComponentInstance? Label { get; set; }

    // Locked as an instance is: no command repositions, restyles or relabels it.
    public bool Locked { get; set; }

    public string? Color
    {
        get => _color;
        set => _color = string.IsNullOrEmpty(value) ? null : value;
    }

    public Edge(
        IEdgeEndpoint source,
        IEdgeEndpoint target,
        Guid? id = null,
        EdgeRouting routingStyle = EdgeRouting.Straight,
        ArrowStyle sourceArrow = ArrowStyle.None,
        ArrowStyle targetArrow = ArrowStyle.Arrow,
        ComponentInstance? label = null,
        string? color = null,
        bool locked = false
    )
    {
        Id = id ?? Guid.NewGuid();
        Source = source;
        Target = target;
        RoutingStyle = routingStyle;
        SourceArrow = sourceArrow;
        TargetArrow = targetArrow;
        Label = label;
        Color = color;
        Locked = locked;
    }
}
