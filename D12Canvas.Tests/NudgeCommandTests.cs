using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class NudgeCommandTests
{
    private static ComponentInstance MakeInstance(double x, double y) =>
        new("sticky-note", new TestProps(), new Bounds(x, y, 50, 50));

    [Fact]
    public void ApplyOffsetsEveryTargetByTheGivenDelta()
    {
        var first = MakeInstance(0, 0);
        var second = MakeInstance(100, 0);
        var command = new NudgeCommand(new[] { first, second }, deltaX: 5, deltaY: -2);

        command.Apply();

        Assert.Equal(new Bounds(5, -2, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(105, -2, 50, 50), second.Bounds);
    }

    [Fact]
    public void UndoRestoresEveryTargetsOriginalBounds()
    {
        var first = MakeInstance(0, 0);
        var second = MakeInstance(100, 0);
        var command = new NudgeCommand(new[] { first, second }, deltaX: 5, deltaY: -2);
        command.Apply();

        command.Undo();

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(100, 0, 50, 50), second.Bounds);
    }

    [Fact]
    public void ExtendAccumulatesTheDeltaRelativeToTheOriginalBoundsNotTheCurrentOnes()
    {
        var instance = MakeInstance(0, 0);
        var command = new NudgeCommand(new[] { instance }, deltaX: 1, deltaY: 0);
        command.Apply();

        command.Extend(deltaX: 1, deltaY: 0);
        command.Extend(deltaX: 1, deltaY: 0);

        Assert.Equal(new Bounds(3, 0, 50, 50), instance.Bounds);
    }

    [Fact]
    public void UndoAfterExtendingRevertsTheWholeAccumulatedBurstInOneStep()
    {
        var instance = MakeInstance(0, 0);
        var command = new NudgeCommand(new[] { instance }, deltaX: 1, deltaY: 0);
        command.Apply();
        command.Extend(deltaX: 1, deltaY: 0);
        command.Extend(deltaX: 1, deltaY: 0);

        command.Undo();

        Assert.Equal(new Bounds(0, 0, 50, 50), instance.Bounds);
    }

    [Fact]
    public void MatchesIsTrueForTheExactSameSetOfTargets()
    {
        var first = MakeInstance(0, 0);
        var second = MakeInstance(100, 0);
        var command = new NudgeCommand(new[] { first, second }, deltaX: 1, deltaY: 0);

        Assert.True(command.Matches(new[] { second, first }));
    }

    [Fact]
    public void MatchesIsFalseWhenTheSelectionSizeDiffers()
    {
        var first = MakeInstance(0, 0);
        var second = MakeInstance(100, 0);
        var command = new NudgeCommand(new[] { first, second }, deltaX: 1, deltaY: 0);

        Assert.False(command.Matches(new[] { first }));
    }

    [Fact]
    public void MatchesIsFalseWhenTheSelectionIsADifferentInstance()
    {
        var first = MakeInstance(0, 0);
        var other = MakeInstance(200, 0);
        var command = new NudgeCommand(new[] { first }, deltaX: 1, deltaY: 0);

        Assert.False(command.Matches(new[] { other }));
    }
}
