using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

// A live group's member ids always resolve, and a group always has two or more members. The
// planner works out the repaired shape of a set of groups against whatever instance ids still
// resolve; the delete composition and both load paths then write that shape.
public class GroupRepairTests
{
    private const string ComponentTypeKey = "test-props";

    private static Func<Guid, bool> Resolving(params Guid[] liveInstanceIds)
    {
        var live = liveInstanceIds.ToHashSet();
        return live.Contains;
    }

    [Fact]
    public void PlanIsEmptyWhenEveryMemberResolvesAndEveryGroupHasTwoOrMore()
    {
        var firstLeaf = Guid.NewGuid();
        var secondLeaf = Guid.NewGuid();
        var thirdLeaf = Guid.NewGuid();
        var inner = new Group([firstLeaf, secondLeaf]);
        var outer = new Group([inner.Id, thirdLeaf]);

        var plan = GroupRepair.Plan([inner, outer], Resolving(firstLeaf, secondLeaf, thirdLeaf));

        Assert.Empty(plan.Removed);
        Assert.Empty(plan.Replaced);
        Assert.Empty(plan.MissingMembers);
    }

    [Fact]
    public void DropsAMemberThatDoesNotResolveAndKeepsTheGroupWhenTwoRemain()
    {
        var firstLeaf = Guid.NewGuid();
        var secondLeaf = Guid.NewGuid();
        var deadLeaf = Guid.NewGuid();
        var group = new Group([firstLeaf, deadLeaf, secondLeaf]);

        var plan = GroupRepair.Plan([group], Resolving(firstLeaf, secondLeaf));

        var replacement = Assert.Single(plan.Replaced);
        Assert.Same(group, replacement.Before);
        Assert.Equal(group.Id, replacement.After.Id);
        Assert.Equal([firstLeaf, secondLeaf], replacement.After.MemberIds);
        Assert.Equal(new MissingMember(group.Id, deadLeaf), Assert.Single(plan.MissingMembers));
        Assert.Empty(plan.Removed);
    }

    [Fact]
    public void DissolvesATopLevelGroupLeftWithOneMember()
    {
        var survivor = Guid.NewGuid();
        var deadLeaf = Guid.NewGuid();
        var group = new Group([survivor, deadLeaf]);

        var plan = GroupRepair.Plan([group], Resolving(survivor));

        Assert.Same(group, Assert.Single(plan.Removed));
        Assert.Equal(new DissolvedGroup(group, survivor), Assert.Single(plan.Dissolved));
        Assert.Empty(plan.Replaced);
        Assert.Empty(plan.Emptied);
    }

    [Fact]
    public void DissolvingANestedGroupPutsTheSurvivorInTheParentWhereTheGroupWas()
    {
        var leafBefore = Guid.NewGuid();
        var survivor = Guid.NewGuid();
        var deadLeaf = Guid.NewGuid();
        var leafAfter = Guid.NewGuid();
        var inner = new Group([survivor, deadLeaf]);
        var parent = new Group([leafBefore, inner.Id, leafAfter]);

        var plan = GroupRepair.Plan([parent, inner], Resolving(leafBefore, survivor, leafAfter));

        Assert.Same(inner, Assert.Single(plan.Removed));
        var replacement = Assert.Single(plan.Replaced);
        Assert.Same(parent, replacement.Before);
        Assert.Equal([leafBefore, survivor, leafAfter], replacement.After.MemberIds);
        Assert.DoesNotContain(plan.MissingMembers, missing => missing.MemberId == inner.Id);
    }

    [Fact]
    public void RemovesAGroupLeftWithNoMembersAndTakesItsIdOutOfTheParent()
    {
        var leafBefore = Guid.NewGuid();
        var firstDeadLeaf = Guid.NewGuid();
        var secondDeadLeaf = Guid.NewGuid();
        var leafAfter = Guid.NewGuid();
        var inner = new Group([firstDeadLeaf, secondDeadLeaf]);
        var parent = new Group([leafBefore, inner.Id, leafAfter]);

        var plan = GroupRepair.Plan([inner, parent], Resolving(leafBefore, leafAfter));

        Assert.Same(inner, Assert.Single(plan.Removed));
        Assert.Same(inner, Assert.Single(plan.Emptied));
        var replacement = Assert.Single(plan.Replaced);
        Assert.Equal([leafBefore, leafAfter], replacement.After.MemberIds);
    }

    [Fact]
    public void EmptyingAnInnerGroupThatLeavesTheParentWithOneDissolvesTheParentToo()
    {
        var survivor = Guid.NewGuid();
        var firstDeadLeaf = Guid.NewGuid();
        var secondDeadLeaf = Guid.NewGuid();
        var inner = new Group([firstDeadLeaf, secondDeadLeaf]);
        var parent = new Group([survivor, inner.Id]);

        var plan = GroupRepair.Plan([inner, parent], Resolving(survivor));

        Assert.Equal(
            new HashSet<Guid> { inner.Id, parent.Id },
            plan.Removed.Select(group => group.Id).ToHashSet()
        );
        Assert.Same(inner, Assert.Single(plan.Emptied));
        Assert.Equal(new DissolvedGroup(parent, survivor), Assert.Single(plan.Dissolved));
        Assert.Empty(plan.Replaced);
    }

    [Fact]
    public void AChainOfDissolvesSettlesTheInnerGroupFirstSoEachReportsTheFinalSurvivor()
    {
        var survivor = Guid.NewGuid();
        var deadLeaf = Guid.NewGuid();
        var inner = new Group([survivor, deadLeaf]);
        var parent = new Group([inner.Id]);

        var plan = GroupRepair.Plan([parent, inner], Resolving(survivor));

        Assert.Equal(
            [new DissolvedGroup(inner, survivor), new DissolvedGroup(parent, survivor)],
            plan.Dissolved
        );
        Assert.Empty(plan.Replaced);
    }

    [Fact]
    public void AMembershipCycleStillTerminates()
    {
        var first = new Group([Guid.NewGuid()]);
        var second = new Group([first.Id]);
        var cyclic = new Group([second.Id, first.Id]);
        var firstWithCycle = new Group([cyclic.Id], first.Id);

        var plan = GroupRepair.Plan([firstWithCycle, second, cyclic], Resolving());

        Assert.NotNull(plan);
    }

    [Fact]
    public void DissolvesAOneMemberGroupEvenWhenItsMemberResolves()
    {
        var onlyMember = Guid.NewGuid();
        var group = new Group([onlyMember]);

        var plan = GroupRepair.Plan([group], Resolving(onlyMember));

        Assert.Same(group, Assert.Single(plan.Removed));
        Assert.Equal(new DissolvedGroup(group, onlyMember), Assert.Single(plan.Dissolved));
        Assert.Empty(plan.MissingMembers);
    }

    [Fact]
    public void ApplyToWritesTheRepairedGroupsOntoTheBoard()
    {
        var board = new Board();
        var leafBefore = AddInstance(board);
        var survivor = AddInstance(board);
        var leafAfter = AddInstance(board);
        var inner = new Group([survivor.Id, Guid.NewGuid()]);
        var parent = new Group([leafBefore.Id, inner.Id, leafAfter.Id]);
        board.AddGroup(inner);
        board.AddGroup(parent);

        GroupRepair.Plan(board.Groups, id => board.GetComponent(id) is not null).ApplyTo(board);

        Assert.Null(board.GetGroup(inner.Id));
        var repairedParent = board.GetGroup(parent.Id);
        Assert.NotNull(repairedParent);
        Assert.Equal([leafBefore.Id, survivor.Id, leafAfter.Id], repairedParent!.MemberIds);
    }

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
}
