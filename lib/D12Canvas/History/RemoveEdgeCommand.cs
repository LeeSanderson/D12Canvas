using D12Canvas.Model;

namespace D12Canvas.History;

public sealed class RemoveEdgeCommand : ICommand
{
    private readonly Board _board;
    private readonly Edge _edge;

    public RemoveEdgeCommand(Board board, Edge edge)
    {
        _board = board;
        _edge = edge;
    }

    public void Apply() => _board.RemoveEdge(_edge.Id);

    public void Undo() => _board.AddEdge(_edge);
}
