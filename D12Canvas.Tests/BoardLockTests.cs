using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

// A lock is a flag on instances and edges; a group holds none and is locked when every member that
// resolves is, nested groups recursively.
public class BoardLockTests
{
    private static ComponentInstance AddShape(Board board, bool locked = false)
    {
        var instance = new ComponentInstance(
            "test-props",
            new TestProps(),
            new Bounds(0, 0, 10, 10),
            locked: locked
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void AnInstanceAndAnEdgeAreLockedByTheirOwnFlag()
    {
        var board = new Board();
        var locked = AddShape(board, locked: true);
        var unlocked = AddShape(board);
        var edge = new Edge(new FloatingEndpoint(0, 0), new FloatingEndpoint(1, 0), locked: true);
        board.AddEdge(edge);

        Assert.True(board.IsLocked(locked.Id));
        Assert.False(board.IsLocked(unlocked.Id));
        Assert.True(board.IsLocked(edge.Id));
    }

    [Fact]
    public void AGroupIsLockedOnlyWhenEveryMemberIs()
    {
        var board = new Board();
        var first = AddShape(board, locked: true);
        var second = AddShape(board, locked: true);
        var third = AddShape(board);
        var allLocked = new Group([first.Id, second.Id]);
        var partlyLocked = new Group([second.Id, third.Id]);
        board.AddGroup(allLocked);
        board.AddGroup(partlyLocked);

        Assert.True(board.IsLocked(allLocked.Id));
        Assert.False(board.IsLocked(partlyLocked.Id));
    }

    [Fact]
    public void ANestedGroupCountsAsLockedWhenAllItsOwnMembersAre()
    {
        var board = new Board();
        var inner = new Group([AddShape(board, locked: true).Id, AddShape(board, locked: true).Id]);
        var outer = new Group([inner.Id, AddShape(board, locked: true).Id]);
        board.AddGroup(inner);
        board.AddGroup(outer);

        Assert.True(board.IsLocked(outer.Id));
    }

    [Fact]
    public void AMemberIdThatNoLongerResolvesIsIgnored()
    {
        var board = new Board();
        var group = new Group([AddShape(board, locked: true).Id, Guid.NewGuid()]);
        board.AddGroup(group);

        Assert.True(board.IsLocked(group.Id));
    }

    [Fact]
    public void AnIdThatResolvesToNothingIsNotLocked()
    {
        Assert.False(new Board().IsLocked(Guid.NewGuid()));
    }
}
