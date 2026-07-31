using D12Canvas.Model;

namespace D12Canvas.History;

// The mirror image of GroupCommand - a thin wrapper over RemoveEntity acting on the Group entity
// itself. Undo re-adds the same Group reference, restoring its MemberIds intact.
public sealed class UngroupCommand : ICommand
{
    private readonly Board _board;
    private readonly Group _group;

    public UngroupCommand(Board board, Group group)
    {
        _board = board;
        _group = group;
    }

    public void Apply() => _board.RemoveGroup(_group.Id);

    public void Undo() => _board.AddGroup(_group);
}
