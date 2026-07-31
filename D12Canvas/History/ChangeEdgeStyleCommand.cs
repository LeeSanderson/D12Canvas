using D12Canvas.Model;

namespace D12Canvas.History;

// Routing style and arrowheads change as one undoable gesture, the Edge counterpart to
// ChangeBoundsCommand - RoutingStyle/SourceArrow/TargetArrow are bundled the same way Bounds
// bundles X/Y/Width/Height, regardless of how many of the three a given gesture actually touched.
public sealed class ChangeEdgeStyleCommand : ICommand
{
    private readonly Edge _edge;
    private readonly EdgeStyle _before;
    private readonly EdgeStyle _after;

    public ChangeEdgeStyleCommand(Edge edge, EdgeStyle before, EdgeStyle after)
    {
        _edge = edge;
        _before = before;
        _after = after;
    }

    public void Apply() => Set(_after);

    public void Undo() => Set(_before);

    private void Set(EdgeStyle style)
    {
        _edge.RoutingStyle = style.RoutingStyle;
        _edge.SourceArrow = style.SourceArrow;
        _edge.TargetArrow = style.TargetArrow;
    }
}
