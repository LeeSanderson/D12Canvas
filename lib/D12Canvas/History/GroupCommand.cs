using D12Canvas.Model;

namespace D12Canvas.History;

public sealed class GroupCommand : ICommand
{
    private readonly Board _board;
    private readonly Group _group;
    private readonly ParentMembership _parent;

    public GroupCommand(Board board, Group group, Guid? parentId = null)
    {
        _board = board;
        _group = group;
        _parent = new ParentMembership(board, parentId);
    }

    public void Apply()
    {
        _board.AddGroup(_group);
        _parent.Nest(_group);
    }

    public void Undo()
    {
        _parent.Restore();
        _board.RemoveGroup(_group.Id);
    }
}
