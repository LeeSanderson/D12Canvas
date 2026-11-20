using D12Canvas.Model;

namespace D12Canvas.History;

public sealed class AddEdgeCommand : ICommand
{
    private readonly Board _board;
    private readonly Edge _edge;

    public AddEdgeCommand(Board board, Edge edge)
    {
        _board = board;
        _edge = edge;
    }

    public void Apply() => _board.AddEdge(_edge);

    public void Undo() => _board.RemoveEdge(_edge.Id);
}
