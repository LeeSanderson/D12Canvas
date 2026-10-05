using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// A press on an edge or its label has no active phase. A release below the threshold selects the
// edge, a double-press adds a label to a bare edge or opens the label's editor, and a press that
// crossed the threshold is abandoned, as a native button is.
public class SelectEdgeGestureTests
{
    private static readonly Guid EdgeId = Guid.NewGuid();

    private static SelectEdgeGesture Press(
        FakeGestureContext context,
        string role,
        int pressCount = 1
    )
    {
        var gesture = new SelectEdgeGesture(
            PointerEvents.Press(
                role,
                PointerPress.PrimaryButton,
                100,
                100,
                EdgeId,
                pressCount: pressCount
            ),
            context
        );
        gesture.Begin();
        return gesture;
    }

    private static PointerRelease ReleaseAt(double x, double y) =>
        PointerEvents.Release(PointerPress.PrimaryButton, x, y);

    [Theory]
    [InlineData(HitRole.Edge)]
    [InlineData(HitRole.EdgeLabel)]
    public void AClickSelectsTheEdgeOnRelease(string role)
    {
        var context = new FakeGestureContext(new Board());
        context.SelectedInstanceIds.Add(Guid.NewGuid());

        var gesture = Press(context, role);
        Assert.Null(context.SelectedEdgeId);

        gesture.Release(ReleaseAt(100, 100));

        Assert.Equal(EdgeId, context.SelectedEdgeId);
        Assert.Empty(context.SelectedInstanceIds);
    }

    [Fact]
    public void APressThatCrossesTheThresholdIsAbandoned()
    {
        var context = new FakeGestureContext(new Board());

        var gesture = Press(context, HitRole.Edge);
        gesture.Move(PointerEvents.Move(300, 100));
        gesture.Release(ReleaseAt(300, 100));

        Assert.Null(context.SelectedEdgeId);
        Assert.Empty(context.LabelsAdded);
    }

    [Fact]
    public void ADoublePressOnTheLineAddsALabel()
    {
        var context = new FakeGestureContext(new Board());

        Press(context, HitRole.Edge, pressCount: 2).Release(ReleaseAt(100, 100));

        Assert.Equal(EdgeId, Assert.Single(context.LabelsAdded));
        Assert.Empty(context.LabelEditRequests);
    }

    [Fact]
    public void ADoublePressOnTheLabelOpensItsEditor()
    {
        var context = new FakeGestureContext(new Board());

        Press(context, HitRole.EdgeLabel, pressCount: 2).Release(ReleaseAt(100, 100));

        Assert.Equal(EdgeId, Assert.Single(context.LabelEditRequests));
        Assert.Empty(context.LabelsAdded);
    }
}
