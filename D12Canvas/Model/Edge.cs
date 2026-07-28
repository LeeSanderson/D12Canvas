namespace D12Canvas.Model;

// ADR 0005: a Board entity connecting two ports. Ticket 48 only creates port-to-port edges with
// straight-line routing - per-edge routing/arrows (ticket 52) and floating endpoints (ticket 49)
// are later tickets' concern.
public sealed class Edge
{
    public Guid Id { get; }
    public PortEndpoint Source { get; set; }
    public PortEndpoint Target { get; set; }

    public Edge(PortEndpoint source, PortEndpoint target, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Source = source;
        Target = target;
    }
}
