using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ResizeStepCommandTests
{
    private static ComponentInstance MakeInstance(
        double x,
        double y,
        double width,
        double height
    ) => new("sticky-note", new TestProps(), new Bounds(x, y, width, height));

    [Fact]
    public void RightGrowsWidthAnchoringTheLeftEdge()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(instance, ResizeDirection.Right, deltaX: 10, deltaY: 0);

        command.Apply();

        Assert.Equal(new Bounds(100, 100, 210, 150), instance.Bounds);
    }

    [Fact]
    public void LeftGrowsWidthAnchoringTheRightEdge()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(instance, ResizeDirection.Left, deltaX: -10, deltaY: 0);

        command.Apply();

        Assert.Equal(new Bounds(90, 100, 210, 150), instance.Bounds);
        Assert.Equal(300, instance.Bounds.Right);
    }

    [Fact]
    public void BottomGrowsHeightAnchoringTheTopEdge()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(
            instance,
            ResizeDirection.Bottom,
            deltaX: 0,
            deltaY: 10
        );

        command.Apply();

        Assert.Equal(new Bounds(100, 100, 200, 160), instance.Bounds);
    }

    [Fact]
    public void TopGrowsHeightAnchoringTheBottomEdge()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(instance, ResizeDirection.Top, deltaX: 0, deltaY: -10);

        command.Apply();

        Assert.Equal(new Bounds(100, 90, 200, 160), instance.Bounds);
        Assert.Equal(250, instance.Bounds.Bottom);
    }

    [Fact]
    public void UndoRestoresTheOriginalBounds()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(instance, ResizeDirection.Right, deltaX: 10, deltaY: 0);
        command.Apply();

        command.Undo();

        Assert.Equal(new Bounds(100, 100, 200, 150), instance.Bounds);
    }

    [Fact]
    public void ExtendAccumulatesTheDeltaRelativeToTheOriginalBoundsNotTheCurrentOnes()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(instance, ResizeDirection.Right, deltaX: 1, deltaY: 0);
        command.Apply();

        command.Extend(deltaX: 1, deltaY: 0);
        command.Extend(deltaX: 1, deltaY: 0);

        Assert.Equal(new Bounds(100, 100, 203, 150), instance.Bounds);
    }

    // The anchor edge (Right, here) must stay exactly fixed across an accumulated burst, not just
    // a single step - proving Extend re-derives from the original bounds rather than compounding
    // rounding/drift step over step.
    [Fact]
    public void TheAnchorEdgeStaysExactlyFixedAcrossAnAccumulatedBurst()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(instance, ResizeDirection.Left, deltaX: -1, deltaY: 0);
        command.Apply();

        command.Extend(deltaX: -1, deltaY: 0);
        command.Extend(deltaX: -1, deltaY: 0);

        Assert.Equal(300, instance.Bounds.Right);
    }

    [Fact]
    public void UndoAfterExtendingRevertsTheWholeAccumulatedBurstInOneStep()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(instance, ResizeDirection.Right, deltaX: 1, deltaY: 0);
        command.Apply();
        command.Extend(deltaX: 1, deltaY: 0);
        command.Extend(deltaX: 1, deltaY: 0);

        command.Undo();

        Assert.Equal(new Bounds(100, 100, 200, 150), instance.Bounds);
    }

    [Fact]
    public void ShrinkingNeverGoesBelowTheMinimumSize()
    {
        var instance = MakeInstance(100, 100, 50, 50);
        var command = new ResizeStepCommand(
            instance,
            ResizeDirection.Right,
            deltaX: -100,
            deltaY: 0
        );

        command.Apply();

        Assert.Equal(50, instance.Bounds.Width);
        Assert.Equal(100, instance.Bounds.X);
    }

    [Fact]
    public void ShrinkingClampedToTheMinimumSizeStillAnchorsTheOppositeEdgeExactly()
    {
        var instance = MakeInstance(100, 100, 50, 50);
        var command = new ResizeStepCommand(instance, ResizeDirection.Left, deltaX: 100, deltaY: 0);

        command.Apply();

        Assert.Equal(50, instance.Bounds.Width);
        Assert.Equal(150, instance.Bounds.Right);
    }

    [Fact]
    public void MatchesIsTrueForTheSameTargetAndDirection()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(instance, ResizeDirection.Right, deltaX: 1, deltaY: 0);

        Assert.True(command.Matches(instance, ResizeDirection.Right));
    }

    [Fact]
    public void MatchesIsFalseWhenTheDirectionDiffers()
    {
        var instance = MakeInstance(100, 100, 200, 150);
        var command = new ResizeStepCommand(instance, ResizeDirection.Right, deltaX: 1, deltaY: 0);

        Assert.False(command.Matches(instance, ResizeDirection.Left));
    }

    [Fact]
    public void MatchesIsFalseForADifferentInstance()
    {
        var first = MakeInstance(100, 100, 200, 150);
        var other = MakeInstance(300, 100, 200, 150);
        var command = new ResizeStepCommand(first, ResizeDirection.Right, deltaX: 1, deltaY: 0);

        Assert.False(command.Matches(other, ResizeDirection.Right));
    }
}
