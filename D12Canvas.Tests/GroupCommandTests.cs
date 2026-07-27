using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class GroupCommandTests
{
    [Fact]
    public void ApplyAddsTheGroupToTheBoard()
    {
        var board = new Board();
        var group = new Group(new[] { Guid.NewGuid(), Guid.NewGuid() });
        var command = new GroupCommand(board, group);

        command.Apply();

        Assert.Same(group, board.GetGroup(group.Id));
    }

    [Fact]
    public void UndoRemovesTheGroupFromTheBoard()
    {
        var board = new Board();
        var group = new Group(new[] { Guid.NewGuid(), Guid.NewGuid() });
        var command = new GroupCommand(board, group);
        command.Apply();

        command.Undo();

        Assert.Null(board.GetGroup(group.Id));
    }

    [Fact]
    public void RedoRestoresTheSameGroupIdentityAndMemberIds()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var board = new Board();
        var group = new Group(new[] { first, second });
        var command = new GroupCommand(board, group);
        command.Apply();
        command.Undo();

        command.Apply(); // redo

        var restored = board.GetGroup(group.Id);
        Assert.Same(group, restored);
        Assert.Equal(new[] { first, second }, restored!.MemberIds);
    }
}
