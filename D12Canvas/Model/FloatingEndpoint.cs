namespace D12Canvas.Model;

// An edge endpoint left unattached at a fixed board point - produced when a connector drag is
// released over empty canvas, or when an attached endpoint is dragged off its port. Resolves to
// itself always (Board.ResolveEndpoint); unlike PortEndpoint it tracks nothing.
public readonly record struct FloatingEndpoint(double X, double Y) : IEdgeEndpoint;
