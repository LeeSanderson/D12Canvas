namespace D12Canvas.Model;

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
