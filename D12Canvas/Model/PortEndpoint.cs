namespace D12Canvas.Model;

// ADR 0005: an edge endpoint attached to one of a component instance's standard (or custom, once
// runtime-added ports exist) ports. The other IEdgeEndpoint shape is FloatingEndpoint (ticket 49).
public readonly record struct PortEndpoint(Guid ComponentId, PortId PortId) : IEdgeEndpoint;
