namespace D12Canvas.Model;

// An edge endpoint attached to one of a component instance's four standard ports. A
// runtime-added custom port is CustomPortEndpoint instead - a separate IEdgeEndpoint shape, not a
// widening of this one's PortId field. The other IEdgeEndpoint shapes are FloatingEndpoint and
// CustomPortEndpoint.
public readonly record struct PortEndpoint(Guid ComponentId, PortId PortId) : IEdgeEndpoint;
