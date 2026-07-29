using D12Canvas.Model;

namespace D12Canvas.History;

// Ticket 52: the three settable Edge properties a routing/arrowhead gesture can change, bundled as
// one before/after snapshot for ChangeEdgeStyleCommand - the same reasoning ChangeBoundsCommand
// already applies to Bounds, rather than passing RoutingStyle/SourceArrow/TargetArrow as three loose
// parameters that always travel together.
public readonly record struct EdgeStyle(
    EdgeRouting RoutingStyle,
    ArrowStyle SourceArrow,
    ArrowStyle TargetArrow
);
