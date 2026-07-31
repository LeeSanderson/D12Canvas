using D12Canvas.Model;

namespace D12Canvas.Persistence;

internal sealed record BoardEnvelope(
    int SchemaVersion,
    IReadOnlyList<ComponentInstanceEnvelope> Components,
    IReadOnlyList<GroupEnvelope>? Groups = null,
    IReadOnlyList<EdgeEnvelope>? Edges = null
);

// CustomPorts defaults to null so a board saved before this property existed - missing it
// entirely - still deserializes, same "field didn't exist yet" convention the Edge-level fields
// below already rely on. A present-but-empty list and an absent property both map to "no custom
// ports" (ComponentInstance's own ctor treats a null list the same as an empty one).
internal sealed record ComponentInstanceEnvelope(
    Guid Id,
    string ComponentTypeKey,
    object? Props,
    BoundsEnvelope Bounds,
    int ZIndex,
    IReadOnlyList<PortDefEnvelope>? CustomPorts = null
);

internal readonly record struct PortDefEnvelope(Guid Id, double FractionX, double FractionY);

internal sealed record GroupEnvelope(Guid Id, IReadOnlyList<Guid> MemberIds);

// An edge's endpoint round-trips as either an instance + standard port reference (PortEndpoint),
// an instance + custom port reference (CustomPortEndpoint), or a board point (FloatingEndpoint) -
// never a CLR-type discriminator, same convention as ComponentInstanceEnvelope's Props. Exactly
// one combination of fields is populated; which one tells FromEndpointEnvelope which
// IEdgeEndpoint shape to rebuild.
//
// RoutingStyle/SourceArrow/TargetArrow default to Edge's own defaults (Straight/None/Arrow) so a
// board saved before these properties existed - missing them entirely - still deserializes, same
// "field didn't exist yet" convention Groups/Edges themselves already use.
//
// Label defaults to null the same way - an edge saved before this property existed (or one that
// simply never had a label added) has no "Label" property at all, and should still deserialize
// with a null label rather than throwing.
internal sealed record EdgeEnvelope(
    Guid Id,
    EdgeEndpointEnvelope Source,
    EdgeEndpointEnvelope Target,
    EdgeRouting RoutingStyle = EdgeRouting.Straight,
    ArrowStyle SourceArrow = ArrowStyle.None,
    ArrowStyle TargetArrow = ArrowStyle.Arrow,
    ComponentInstanceEnvelope? Label = null
);

// CustomPortId defaults to null (last positional, same "field didn't exist yet" tolerance the
// rest of this envelope already relies on) so a board saved before this field existed still
// deserializes every PortEndpoint/FloatingEndpoint it has unaffected.
internal sealed record EdgeEndpointEnvelope(
    Guid? ComponentId,
    PortId? PortId,
    double? X,
    double? Y,
    Guid? CustomPortId = null
);

internal readonly record struct BoundsEnvelope(double X, double Y, double Width, double Height);
