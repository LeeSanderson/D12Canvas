using D12Canvas.Model;

namespace D12Canvas.History;

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
