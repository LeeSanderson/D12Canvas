using D12Canvas.Model;

namespace D12Canvas.History;

// Grouping and ungrouping inside another group rewrite that parent's member list, so a nested
// group takes its members' place and an ungrouped one gives its members back in the same spot.
// The parent is read by id when the edit is applied, so several edits to one parent in a single
// composite each see the one before. Undo puts back the exact parent the edit replaced.
internal sealed class ParentMembership
{
    private readonly Board _board;
    private readonly Guid? _parentId;
    private Group? _parentBefore;

    public ParentMembership(Board board, Guid? parentId)
    {
        _board = board;
        _parentId = parentId;
    }

    public void Nest(Group group) =>
        Rewrite(members =>
        {
            var nested = group.MemberIds.ToHashSet();
            var position = members.FindIndex(nested.Contains);
            if (position < 0)
            {
                return null;
            }

            var rewritten = members.Where(id => !nested.Contains(id)).ToList();
            rewritten.Insert(position, group.Id);
            return rewritten;
        });

    public void Splice(Group group) =>
        Rewrite(members =>
        {
            var position = members.IndexOf(group.Id);
            if (position < 0)
            {
                return null;
            }

            var rewritten = members.ToList();
            rewritten.RemoveAt(position);
            rewritten.InsertRange(position, group.MemberIds);
            return rewritten;
        });

    public void Restore()
    {
        if (_parentBefore is not { } before)
        {
            return;
        }

        _board.RemoveGroup(before.Id);
        _board.AddGroup(before);
        _parentBefore = null;
    }

    private void Rewrite(Func<List<Guid>, List<Guid>?> rewrite)
    {
        if (_parentId is not { } parentId || _board.GetGroup(parentId) is not { } parent)
        {
            return;
        }

        if (rewrite(parent.MemberIds.ToList()) is not { } members)
        {
            return;
        }

        _parentBefore = parent;
        _board.RemoveGroup(parent.Id);
        _board.AddGroup(new Group(members, parent.Id));
    }
}
