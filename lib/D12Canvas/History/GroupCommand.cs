using D12Canvas.Model;

namespace D12Canvas.History;

// Adds the Group entity; its members are referenced by id and never mutated. Grouping inside a
// parent group also replaces the grouped ids in the parent's member list with the new group's id,
// at the position of the first of them, in the same entry.
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
