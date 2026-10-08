using D12Canvas.Model;

namespace D12Canvas.History;

// The mirror image of GroupCommand. Undo re-adds the same Group reference, restoring its MemberIds
// intact. Ungrouping a group nested in a parent splices its members back into the parent's member
// list where the group was, in the same entry.
public sealed class UngroupCommand : ICommand
{
    private readonly Board _board;
    private readonly Group _group;
    private readonly ParentMembership _parent;

    public UngroupCommand(Board board, Group group, Guid? parentId = null)
    {
        _board = board;
        _group = group;
        _parent = new ParentMembership(board, parentId);
    }

    public void Apply()
    {
        _parent.Splice(_group);
        _board.RemoveGroup(_group.Id);
    }

    public void Undo()
    {
        _board.AddGroup(_group);
        _parent.Restore();
    }
}
