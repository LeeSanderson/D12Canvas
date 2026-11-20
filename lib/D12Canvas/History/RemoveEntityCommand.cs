using D12Canvas.Model;

namespace D12Canvas.History;

public sealed class RemoveEntityCommand : ICommand
{
    private readonly Board _board;
    private readonly ComponentInstance _instance;

    public RemoveEntityCommand(Board board, ComponentInstance instance)
    {
        _board = board;
        _instance = instance;
    }

    public void Apply() => _board.RemoveComponent(_instance.Id);

    public void Undo() => _board.AddComponent(_instance);
}
