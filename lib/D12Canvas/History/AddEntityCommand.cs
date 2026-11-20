using D12Canvas.Model;

namespace D12Canvas.History;

public sealed class AddEntityCommand : ICommand
{
    private readonly Board _board;
    private readonly ComponentInstance _instance;

    public AddEntityCommand(Board board, ComponentInstance instance)
    {
        _board = board;
        _instance = instance;
    }

    public void Apply() => _board.AddComponent(_instance);

    public void Undo() => _board.RemoveComponent(_instance.Id);
}
