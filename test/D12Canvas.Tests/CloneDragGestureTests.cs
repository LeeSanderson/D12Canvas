using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// Alt on a MoveSelection drags a copy of the selection and leaves the originals where they are.
// The copy lives in the preview's pending fragment, Alt is read on every move, and the release
// commits whichever of the two the last move published.
public class CloneDragGestureTests
{
    private static ComponentInstance AddInstance(Board board, double x, double y, int zIndex = 0)
    {
        var instance = new ComponentInstance(
            "test-props",
            new TestProps(),
            new Bounds(x, y, 50, 50),
            zIndex
        );
        board.AddComponent(instance);
        return instance;
    }

    private static MoveSelectionGesture PressInstance(
        FakeGestureContext context,
        Guid entityId,
        double x,
        double y
    )
    {
        var gesture = new MoveSelectionGesture(
            PointerEvents.Press(HitRole.Instance, PointerPress.PrimaryButton, x, y, entityId),
            context
        );
        gesture.Begin();
        return gesture;
    }

    private static PointerRelease ReleaseAt(double x, double y) =>
        PointerEvents.Release(PointerPress.PrimaryButton, x, y);

    [Fact]
    public void AnAltMovePublishesACopyAtTheDeltaAndLeavesTheOriginalOutOfThePreview()
    {
        var board = new Board();
        var original = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);
        var gesture = PressInstance(context, original.Id, 10, 10);

        gesture.Move(PointerEvents.Move(110, 60, alt: true));

        var copy = Assert.Single(context.PendingFragment!.Components);
        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal(original.Bounds, copy.Bounds);
        Assert.Equal(
            new Dictionary<Guid, Bounds> { [copy.Id] = new Bounds(100, 50, 50, 50) },
            context.Preview
        );
        Assert.Equal(new Bounds(0, 0, 50, 50), original.Bounds);
    }

    [Fact]
    public void AnAltClickBuildsNoCopy()
    {
        var board = new Board();
        var original = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);
        var gesture = PressInstance(context, original.Id, 10, 10);

        gesture.Release(ReleaseAt(10, 10) with { AltKey = true });

        Assert.Equal(0, context.CopiesBuilt);
        Assert.Null(context.PendingFragment);
    }

    [Fact]
    public void ReleasingAltPutsTheDeltaBackOnTheOriginalsAndPressingItAgainShowsTheSameCopy()
    {
        var board = new Board();
        var original = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);
        var gesture = PressInstance(context, original.Id, 10, 10);

        gesture.Move(PointerEvents.Move(110, 60, alt: true));
        var copy = Assert.Single(context.PendingFragment!.Components);

        gesture.Move(PointerEvents.Move(110, 60));
        Assert.Null(context.PendingFragment);
        Assert.Equal(
            new Dictionary<Guid, Bounds> { [original.Id] = new Bounds(100, 50, 50, 50) },
            context.Preview
        );

        gesture.Move(PointerEvents.Move(120, 60, alt: true));
        Assert.Same(copy, Assert.Single(context.PendingFragment!.Components));
        Assert.Equal(
            new Dictionary<Guid, Bounds> { [copy.Id] = new Bounds(110, 50, 50, 50) },
            context.Preview
        );
        Assert.Equal(1, context.CopiesBuilt);
    }

    [Fact]
    public void AReleaseWhileCloningCommitsTheCopiesWhereTheyWereLastShown()
    {
        var board = new Board();
        var original = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);
        var gesture = PressInstance(context, original.Id, 10, 10);

        gesture.Move(PointerEvents.Move(110, 60, alt: true));
        gesture.Release(ReleaseAt(110, 60));

        var committed = Assert.Single(context.CommittedFragments);
        var copy = Assert.Single(committed!.Components);
        Assert.Equal(new Bounds(100, 50, 50, 50), Assert.Single(context.Commits)[copy.Id]);
    }

    [Fact]
    public void LettingGoOfAltBeforeTheReleaseCommitsAMove()
    {
        var board = new Board();
        var original = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);
        var gesture = PressInstance(context, original.Id, 10, 10);

        gesture.Move(PointerEvents.Move(110, 60, alt: true));
        gesture.Move(PointerEvents.Move(110, 60));
        gesture.Release(ReleaseAt(110, 60) with { AltKey = true });

        Assert.Null(Assert.Single(context.CommittedFragments));
        Assert.Equal(new Bounds(100, 50, 50, 50), Assert.Single(context.Commits)[original.Id]);
    }

    [Fact]
    public void TheCopyCarriesTheEdgeBetweenTwoSelectedShapesAndAFloatingEndMovesWithTheDelta()
    {
        var board = new Board();
        var left = AddInstance(board, 0, 0);
        var right = AddInstance(board, 200, 0);
        var interior = new Edge(
            new PortEndpoint(left.Id, PortId.Right),
            new PortEndpoint(right.Id, PortId.Left)
        );
        var loose = new Edge(
            new PortEndpoint(right.Id, PortId.Bottom),
            new FloatingEndpoint(225, 200)
        );
        board.AddEdge(interior);
        board.AddEdge(loose);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.UnionWith([left.Id, right.Id]);
        context.SelectedEdgeIds.Add(loose.Id);
        var gesture = PressInstance(context, left.Id, 10, 10);

        gesture.Move(PointerEvents.Move(10, 110, alt: true));

        var copies = context.PendingFragment!;
        Assert.Equal(2, copies.Components.Count);
        Assert.Equal(2, copies.Edges.Count);
        Assert.DoesNotContain(copies.Edges, edge => edge.Id == interior.Id || edge.Id == loose.Id);
        var copiedLoose = copies.Edges.Single(edge => edge.Target is FloatingEndpoint);
        Assert.Equal(
            new FloatingEndpoint(225, 300),
            context.MovedEndpoints[new EdgeEnd(copiedLoose.Id, IsSource: false)]
        );
        Assert.DoesNotContain(new EdgeEnd(loose.Id, IsSource: false), context.MovedEndpoints.Keys);
    }

    [Fact]
    public void TheCopiesAreStackedAboveEverythingOnTheBoard()
    {
        var board = new Board();
        var original = AddInstance(board, 0, 0, zIndex: 1);
        AddInstance(board, 300, 300, zIndex: 7);
        var context = new FakeGestureContext(board);
        var gesture = PressInstance(context, original.Id, 10, 10);

        gesture.Move(PointerEvents.Move(110, 60, alt: true));

        Assert.Equal(8, Assert.Single(context.PendingFragment!.Components).ZIndex);
    }

    [Fact]
    public void TheOriginalIsASnapCandidateForItsOwnCopy()
    {
        var board = new Board();
        var original = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board) { ObjectSnapping = true };
        var gesture = PressInstance(context, original.Id, 10, 10);

        gesture.Move(PointerEvents.Move(13, 210, alt: true));

        var copy = Assert.Single(context.PendingFragment!.Components);
        Assert.Equal(new Bounds(0, 200, 50, 50), context.Preview[copy.Id]);
        Assert.Contains(new AlignmentGuide(SnapAxis.X, 0), context.Guides);
    }
}
