using D12Canvas.Model;

namespace D12Canvas.History;

// Creation, undoable. The instance already carries its full state (id, bounds, props), so
// undo/redo just removes/re-adds the same reference - no snapshot needed.
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
