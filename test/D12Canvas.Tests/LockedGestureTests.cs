using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// A gesture over a selection that holds locked entities acts on the unlocked ones and leaves the
// locked ones exactly as they are; nothing a gesture sweeps, carries or drops onto is locked.
public class LockedGestureTests
{
    private static ComponentInstance AddInstance(Board board, Bounds bounds, bool locked = false)
    {
        var instance = new ComponentInstance("test-props", new TestProps(), bounds, locked: locked);
        board.AddComponent(instance);
        return instance;
    }

    private static T Begun<T>(T gesture)
        where T : PointerGesture
    {
        gesture.Begin();
        return gesture;
    }

    private static MoveSelectionGesture PressToMove(FakeGestureContext context, Guid entityId) =>
        Begun(
            new MoveSelectionGesture(
                PointerEvents.Press(HitRole.Instance, PointerPress.PrimaryButton, 10, 10, entityId),
                context
            )
        );

    private static DragEdgeEndGesture PressEdgeEnd(
        FakeGestureContext context,
        string role,
        Guid entityId,
        string part,
        double x,
        double y
    ) =>
        Begun(
            new DragEdgeEndGesture(
                PointerEvents.Press(role, PointerPress.PrimaryButton, x, y, entityId, part: part),
                context
            )
        );

    [Fact]
    public void AMoveCarriesTheUnlockedMembersAndLeavesTheLockedOneOutOfThePreview()
    {
        var board = new Board();
        var free = AddInstance(board, new Bounds(0, 0, 50, 50));
        var locked = AddInstance(board, new Bounds(100, 0, 50, 50), locked: true);
        var group = new Group([free.Id, locked.Id]);
        board.AddGroup(group);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(group.Id);
        var gesture = PressToMove(context, free.Id);

        gesture.Move(PointerEvents.Move(40, 30));
        gesture.Release(PointerEvents.Release(PointerPress.PrimaryButton, 40, 30));

        var committed = context.Commits.Single();
        Assert.Equal(new Bounds(30, 20, 50, 50), committed[free.Id]);
        Assert.False(committed.ContainsKey(locked.Id));
    }

    [Fact]
    public void AMoveSnapsTheUnlockedBoxToTheLockedMemberLeftBehind()
    {
        var board = new Board();
        var free = AddInstance(board, new Bounds(0, 0, 50, 50));
        var locked = AddInstance(board, new Bounds(200, 0, 50, 50), locked: true);
        var group = new Group([free.Id, locked.Id]);
        board.AddGroup(group);
        var context = new FakeGestureContext(board) { ObjectSnapping = true };
        context.SelectedInstanceIds.Add(group.Id);
        var gesture = PressToMove(context, free.Id);

        gesture.Move(PointerEvents.Move(10 + 197, 10 + 100));

        Assert.Equal(200, context.Preview[free.Id].X);
    }

    [Fact]
    public void AMoveLeavesALockedEdgesFloatingEndWhereItIs()
    {
        var board = new Board();
        var free = AddInstance(board, new Bounds(0, 0, 50, 50));
        var edge = new Edge(
            new FloatingEndpoint(0, 100),
            new FloatingEndpoint(50, 100),
            locked: true
        );
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(free.Id);
        context.SelectedEdgeIds.Add(edge.Id);
        var gesture = PressToMove(context, free.Id);

        gesture.Move(PointerEvents.Move(40, 30));

        Assert.Empty(context.MovedEndpoints);
    }

    [Fact]
    public void AResizeKeepsTheRealBoundsAndScalesOnlyTheUnlockedMembersInside()
    {
        var board = new Board();
        var free = AddInstance(board, new Bounds(0, 0, 100, 100));
        var locked = AddInstance(board, new Bounds(100, 0, 100, 100), locked: true);
        var group = new Group([free.Id, locked.Id]);
        board.AddGroup(group);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(group.Id);
        var gesture = Begun(
            new ResizeSelectionGesture(
                PointerEvents.Press(
                    HitRole.SelectionHandle,
                    PointerPress.PrimaryButton,
                    200,
                    50,
                    part: "right"
                ),
                context
            )
        );

        gesture.Move(PointerEvents.Move(400, 50));

        Assert.Equal(new Bounds(0, 0, 200, 100), context.Preview[free.Id]);
        Assert.False(context.Preview.ContainsKey(locked.Id));
        Assert.Equal(new Bounds(0, 0, 400, 100), context.SelectionFrame);
    }

    [Fact]
    public void AResizeWithNothingLockedDrawsNoFrameOfItsOwn()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(0, 0, 100, 100));
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(shape.Id);
        var gesture = Begun(
            new ResizeSelectionGesture(
                PointerEvents.Press(
                    HitRole.ResizeHandle,
                    PointerPress.PrimaryButton,
                    100,
                    50,
                    shape.Id,
                    part: "right"
                ),
                context
            )
        );

        gesture.Move(PointerEvents.Move(200, 50));

        Assert.Null(context.SelectionFrame);
    }

    [Fact]
    public void AMarqueeSweepsNeitherALockedInstanceNorALockedEdge()
    {
        var board = new Board();
        var free = AddInstance(board, new Bounds(0, 0, 50, 50));
        AddInstance(board, new Bounds(100, 0, 50, 50), locked: true);
        var lockedEdge = new Edge(
            new FloatingEndpoint(10, 80),
            new FloatingEndpoint(40, 80),
            locked: true
        );
        var freeEdge = new Edge(new FloatingEndpoint(10, 90), new FloatingEndpoint(40, 90));
        board.AddEdge(lockedEdge);
        board.AddEdge(freeEdge);
        var context = new FakeGestureContext(board);
        var gesture = Begun(
            new MarqueeSelectGesture(
                PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0),
                context
            )
        );

        gesture.Move(PointerEvents.Move(300, 300));

        Assert.Equal([free.Id], context.SelectedInstanceIds);
        Assert.Equal([freeEdge.Id], context.SelectedEdgeIds);
    }

    [Fact]
    public void APortWithALockedEdgePinnedToItPullsANewEdgeInstead()
    {
        var board = new Board();
        var source = AddInstance(board, new Bounds(0, 0, 100, 100));
        var port = new PortEndpoint(source.Id, PortId.Right);
        board.AddEdge(new Edge(port, new FloatingEndpoint(300, 50), locked: true));
        var context = new FakeGestureContext(board);
        var gesture = PressEdgeEnd(context, HitRole.Port, source.Id, "Right", 100, 50);

        gesture.Move(PointerEvents.Move(200, 200));
        gesture.Release(PointerEvents.Release(PointerPress.PrimaryButton, 200, 200));

        Assert.Empty(context.EndpointChanges);
        Assert.Equal(port, context.AddedEdges.Single().Source);
    }

    [Fact]
    public void ALockedEdgesFloatingEndIsNotCarried()
    {
        var board = new Board();
        var locked = new Edge(
            new FloatingEndpoint(0, 0),
            new FloatingEndpoint(100, 0),
            locked: true
        );
        board.AddEdge(locked);
        var context = new FakeGestureContext(board);
        var gesture = PressEdgeEnd(context, HitRole.EdgeEndpoint, locked.Id, "target", 100, 0);

        gesture.Move(PointerEvents.Move(200, 200));
        gesture.Release(PointerEvents.Release(PointerPress.PrimaryButton, 200, 200));

        Assert.Null(context.PendingEdge);
        Assert.Empty(context.EndpointChanges);
    }

    [Fact]
    public void ADropOverALockedShapeLooksThroughItToTheShapeBeneath()
    {
        var board = new Board();
        var source = AddInstance(board, new Bounds(0, 0, 100, 100));
        var beneath = AddInstance(board, new Bounds(300, 0, 100, 100));
        var locked = AddInstance(board, new Bounds(300, 0, 100, 100), locked: true);
        var context = new FakeGestureContext(board);
        var gesture = PressEdgeEnd(context, HitRole.Port, source.Id, "Right", 100, 50);

        gesture.Move(PointerEvents.Move(350, 50));
        gesture.Release(
            PointerEvents.Release(PointerPress.PrimaryButton, 350, 50) with
            {
                Hits =
                [
                    new PointerHit(HitRole.Instance, locked.Id, null),
                    new PointerHit(HitRole.Instance, beneath.Id, null),
                ],
            }
        );

        Assert.Equal(new AutoPortEndpoint(beneath.Id), context.AddedEdges.Single().Target);
    }
}
