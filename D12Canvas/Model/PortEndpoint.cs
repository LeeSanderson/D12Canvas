namespace D12Canvas.Model;

// ADR 0005: an edge endpoint attached to one of a component instance's four standard ports. A
// runtime-added custom port (ticket 55) is CustomPortEndpoint instead - a separate IEdgeEndpoint
// shape, not a widening of this one's PortId field. The other IEdgeEndpoint shapes are
// FloatingEndpoint (ticket 49) and CustomPortEndpoint.
public readonly record struct PortEndpoint(Guid ComponentId, PortId PortId) : IEdgeEndpoint;
