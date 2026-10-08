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

    [Fact]
    public void GroupingInsideAParentReplacesTheGroupedIdsWithTheNewGroupAtTheFirstOfThem()
    {
        var (first, second, third) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var board = new Board();
        var parent = new Group([first, second, third]);
        board.AddGroup(parent);
        var nested = new Group([third, second]);
        var command = new GroupCommand(board, nested, parent.Id);

        command.Apply();

        Assert.Equal([first, nested.Id], board.GetGroup(parent.Id)!.MemberIds);

        command.Undo();

        Assert.Same(parent, board.GetGroup(parent.Id));
        Assert.Null(board.GetGroup(nested.Id));

        command.Apply();

        Assert.Equal([first, nested.Id], board.GetGroup(parent.Id)!.MemberIds);
    }

    [Fact]
    public void UngroupingInsideAParentSplicesTheMembersBackWhereTheGroupWas()
    {
        var (first, second, third) = (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var board = new Board();
        var nested = new Group([second, third]);
        var parent = new Group([nested.Id, first]);
        board.AddGroup(nested);
        board.AddGroup(parent);
        var command = new UngroupCommand(board, nested, parent.Id);

        command.Apply();

        Assert.Null(board.GetGroup(nested.Id));
        Assert.Equal([second, third, first], board.GetGroup(parent.Id)!.MemberIds);

        command.Undo();

        Assert.Same(nested, board.GetGroup(nested.Id));
        Assert.Same(parent, board.GetGroup(parent.Id));
    }
}
