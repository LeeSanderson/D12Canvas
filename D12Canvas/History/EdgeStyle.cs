using D12Canvas.Model;

namespace D12Canvas.History;

// The settable visual properties of an Edge, bundled as one before/after snapshot for
// ChangeEdgeStyleCommand - the same reasoning ChangeBoundsCommand already applies to Bounds,
// rather than passing them as loose parameters that always travel together. A null Color means
// no author opinion: the edge paints in the theme's edge token.
public readonly record struct EdgeStyle(
    EdgeRouting RoutingStyle,
    ArrowStyle SourceArrow,
    ArrowStyle TargetArrow,
    string? Color = null
);
