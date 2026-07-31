using D12Canvas.Model;

namespace D12Canvas.History;

// Named for readability in a history/log context, but a thin wrapper over AddEntity acting on the
// Group entity itself - Group.MemberIds is a reference list held by the group, so grouping never
// mutates the member entities.
public sealed class GroupCommand : ICommand
{
    private readonly Board _board;
    private readonly Group _group;

    public GroupCommand(Board board, Group group)
    {
        _board = board;
        _group = group;
    }

    public void Apply() => _board.AddGroup(_group);

    public void Undo() => _board.RemoveGroup(_group.Id);
}
