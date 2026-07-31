using D12Canvas.Model;

namespace D12Canvas.History;

// Deletion, undoable - the mirror image of AddEntityCommand. Undo re-adds the same instance
// reference, restoring identity, bounds, and props exactly as they were.
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
