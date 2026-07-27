using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class UngroupCommandTests
{
    [Fact]
    public void ApplyRemovesTheGroupFromTheBoard()
    {
        var board = new Board();
        var group = new Group(new[] { Guid.NewGuid(), Guid.NewGuid() });
        board.AddGroup(group);
        var command = new UngroupCommand(board, group);

        command.Apply();

        Assert.Null(board.GetGroup(group.Id));
    }

    [Fact]
    public void UndoRestoresTheSameGroupIdentityAndMemberIds()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var board = new Board();
        var group = new Group(new[] { first, second });
        board.AddGroup(group);
        var command = new UngroupCommand(board, group);
        command.Apply();

        command.Undo();

        var restored = board.GetGroup(group.Id);
        Assert.Same(group, restored);
        Assert.Equal(new[] { first, second }, restored!.MemberIds);
    }

    [Fact]
    public void RedoRemovesTheGroupAgain()
    {
        var board = new Board();
        var group = new Group(new[] { Guid.NewGuid(), Guid.NewGuid() });
        board.AddGroup(group);
        var command = new UngroupCommand(board, group);
        command.Apply();
        command.Undo();

        command.Apply(); // redo

        Assert.Null(board.GetGroup(group.Id));
    }
}
