using D12Canvas.Model;

namespace D12Canvas.History;

public readonly record struct EdgeStyle(
    EdgeRouting RoutingStyle,
    ArrowStyle SourceArrow,
    ArrowStyle TargetArrow,
    string? Color = null
);
