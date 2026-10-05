using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

// Removing instances and repairing the groups they belonged to is one history entry: undo brings
// back the instances, the groups and every parent membership together.
public class InstanceRemovalTests
{
    private const string ComponentTypeKey = "test-props";

    private static ComponentInstance AddInstance(Board board)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void RemovingAMemberOfATwoMemberGroupDissolvesItIntoItsParentInOneEntry()
    {
        var board = new Board();
        var removed = AddInstance(board);
        var survivor = AddInstance(board);
        var sibling = AddInstance(board);
        var inner = new Group([removed.Id, survivor.Id]);
        var parent = new Group([inner.Id, sibling.Id]);
        board.AddGroup(inner);
        board.AddGroup(parent);
        var history = new CommandHistory();

        history.Do(new CompositeCommand(InstanceRemoval.Compose(board, [removed.Id])));

        Assert.Null(board.GetComponent(removed.Id));
        Assert.Null(board.GetGroup(inner.Id));
        Assert.Equal([survivor.Id, sibling.Id], board.GetGroup(parent.Id)!.MemberIds);

        history.Undo();

        Assert.Same(removed, board.GetComponent(removed.Id));
        Assert.Same(inner, board.GetGroup(inner.Id));
        Assert.Same(parent, board.GetGroup(parent.Id));
        Assert.Equal([inner.Id, sibling.Id], board.GetGroup(parent.Id)!.MemberIds);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void RemovingEveryMemberOfAGroupRemovesTheGroupInTheSameEntry()
    {
        var board = new Board();
        var firstMember = AddInstance(board);
        var secondMember = AddInstance(board);
        var group = new Group([firstMember.Id, secondMember.Id]);
        board.AddGroup(group);
        var history = new CommandHistory();

        history.Do(
            new CompositeCommand(InstanceRemoval.Compose(board, [firstMember.Id, secondMember.Id]))
        );

        Assert.Empty(board.Components);
        Assert.Empty(board.Groups);

        history.Undo();

        Assert.Equal(2, board.Components.Count);
        Assert.Same(group, Assert.Single(board.Groups));
    }

    [Fact]
    public void EmptyingANestedGroupRepairsEveryLevelAbove()
    {
        var board = new Board();
        var firstMember = AddInstance(board);
        var secondMember = AddInstance(board);
        var sibling = AddInstance(board);
        var inner = new Group([firstMember.Id, secondMember.Id]);
        var parent = new Group([inner.Id, sibling.Id]);
        board.AddGroup(inner);
        board.AddGroup(parent);
        var history = new CommandHistory();

        history.Do(
            new CompositeCommand(InstanceRemoval.Compose(board, [firstMember.Id, secondMember.Id]))
        );

        Assert.Empty(board.Groups);
        Assert.Same(sibling, Assert.Single(board.Components));

        history.Undo();

        Assert.Equal(2, board.Groups.Count);
        Assert.Equal([inner.Id, sibling.Id], board.GetGroup(parent.Id)!.MemberIds);
    }

    [Fact]
    public void RemovingOneMemberOfAThreeMemberGroupLeavesOnlyLiveIdsAndUndoRestoresTheList()
    {
        var board = new Board();
        var firstMember = AddInstance(board);
        var removed = AddInstance(board);
        var thirdMember = AddInstance(board);
        var group = new Group([firstMember.Id, removed.Id, thirdMember.Id]);
        board.AddGroup(group);
        var history = new CommandHistory();

        history.Do(new CompositeCommand(InstanceRemoval.Compose(board, [removed.Id])));

        Assert.Equal([firstMember.Id, thirdMember.Id], board.GetGroup(group.Id)!.MemberIds);

        history.Undo();

        Assert.Same(group, board.GetGroup(group.Id));
        Assert.Equal([firstMember.Id, removed.Id, thirdMember.Id], group.MemberIds);
        Assert.Same(removed, board.GetComponent(removed.Id));
    }

    [Fact]
    public void RemovingAnUnrelatedInstanceLeavesAGroupAHostBrokeAlone()
    {
        var board = new Board();
        var onlyMember = AddInstance(board);
        var hostBuiltGroup = new Group([onlyMember.Id, Guid.NewGuid()]);
        board.AddGroup(hostBuiltGroup);
        var loose = AddInstance(board);

        var commands = InstanceRemoval.Compose(board, [loose.Id]);

        Assert.IsType<RemoveEntityCommand>(Assert.Single(commands));
    }

    [Fact]
    public void UngroupedInstancesProduceOnlyTheirOwnRemovals()
    {
        var board = new Board();
        var firstMember = AddInstance(board);
        var secondMember = AddInstance(board);
        var group = new Group([firstMember.Id, secondMember.Id]);
        board.AddGroup(group);
        var loose = AddInstance(board);

        var commands = InstanceRemoval.Compose(board, [loose.Id]);

        Assert.IsType<RemoveEntityCommand>(Assert.Single(commands));
    }

    [Fact]
    public void IdsThatAreNotOnTheBoardProduceNothing()
    {
        var board = new Board();

        Assert.Empty(InstanceRemoval.Compose(board, [Guid.NewGuid()]));
    }
}
