using D12Canvas.Model;

namespace D12Canvas.Persistence;

internal sealed record BoardEnvelope(
    int SchemaVersion,
    IReadOnlyList<ComponentInstanceEnvelope> Components,
    IReadOnlyList<GroupEnvelope>? Groups = null,
    IReadOnlyList<EdgeEnvelope>? Edges = null
);

internal sealed record ComponentInstanceEnvelope(
    Guid Id,
    string ComponentTypeKey,
    object? Props,
    BoundsEnvelope Bounds,
    int ZIndex
);

internal sealed record GroupEnvelope(Guid Id, IReadOnlyList<Guid> MemberIds);

// ADR 0005/ticket 51: an edge's endpoint round-trips as either an instance + port reference
// (PortEndpoint) or a board point (FloatingEndpoint, ticket 49) - never a CLR-type discriminator,
// same convention as ComponentInstanceEnvelope's Props. Exactly one pair of fields is populated;
// which pair tells FromEndpointEnvelope which IEdgeEndpoint shape to rebuild.
//
// Ticket 52: RoutingStyle/SourceArrow/TargetArrow default to Edge's own ADR 0005 defaults
// (Straight/None/Arrow) so a board saved before this ticket - missing these properties entirely -
// still deserializes, same "field didn't exist yet" convention Groups/Edges themselves already use.
internal sealed record EdgeEnvelope(
    Guid Id,
    EdgeEndpointEnvelope Source,
    EdgeEndpointEnvelope Target,
    EdgeRouting RoutingStyle = EdgeRouting.Straight,
    ArrowStyle SourceArrow = ArrowStyle.None,
    ArrowStyle TargetArrow = ArrowStyle.Arrow
);

internal sealed record EdgeEndpointEnvelope(
    Guid? ComponentId,
    PortId? PortId,
    double? X,
    double? Y
);

internal readonly record struct BoundsEnvelope(double X, double Y, double Width, double Height);
