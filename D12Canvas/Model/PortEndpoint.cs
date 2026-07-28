namespace D12Canvas.Model;

// ADR 0005: an edge endpoint attached to one of a component instance's standard ports. Ticket 48
// only needs this attached form - a floating (unattached) endpoint is ticket 49's concern, at
// which point Edge.Source/Target will need to widen beyond this single shape.
public readonly record struct PortEndpoint(Guid ComponentId, PortId PortId);
