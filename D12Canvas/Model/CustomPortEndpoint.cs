namespace D12Canvas.Model;

// ADR 0005/ticket 55: an edge endpoint attached to one of an instance's own runtime-added custom
// ports, identified by the port's own Id (ComponentInstance.CustomPorts) rather than a PortId enum
// value. Added as its own IEdgeEndpoint shape - the same way FloatingEndpoint was (ticket 49) -
// rather than widening PortEndpoint's own PortId field to a type that could hold either.
public readonly record struct CustomPortEndpoint(Guid ComponentId, Guid PortId) : IEdgeEndpoint;
