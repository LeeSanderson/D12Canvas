using D12Canvas.Model;

namespace D12Canvas.History;

// Adding, replacing, or removing an edge's embedded Label is one undoable gesture - the Edge
// counterpart to ChangeEdgeStyleCommand, swapping the whole ComponentInstance reference (or null)
// rather than one of its fields. Editing an existing label's own Props (its text) is a separate
// gesture and reuses the generic MutateEntityCommand instead (see
// DiagramCanvas.CommitPropsChange/Board.FindEdgeLabel).
public sealed class ChangeEdgeLabelCommand : ICommand
{
    private readonly Edge _edge;
    private readonly ComponentInstance? _before;
    private readonly ComponentInstance? _after;

    public ChangeEdgeLabelCommand(Edge edge, ComponentInstance? before, ComponentInstance? after)
    {
        _edge = edge;
        _before = before;
        _after = after;
    }

    public void Apply() => _edge.Label = _after;

    public void Undo() => _edge.Label = _before;
}
