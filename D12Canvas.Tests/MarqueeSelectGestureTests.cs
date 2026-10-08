using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// The band is drawn in board space from the press point, every tick replaces the selection with
// what it intersects, Shift at press unions into the press-time selection instead, and a release
// below the threshold is the click on empty canvas that clears the selection.
public class MarqueeSelectGestureTests
{
    private const string ComponentTypeKey = "test-props";

    private static ComponentInstance AddInstance(Board board, double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void TheBandIsTheRectangleBetweenThePressAndTheCurrentPointInBoardSpace()
    {
        var context = new FakeGestureContext(new Board());
        context.ZoomPan.SetPanPosition(100, 100);
        context.ZoomPan.Scale = 2;
        var marquee = new MarqueeSelectGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 300, 300),
            context
        );

        marquee.Move(PointerEvents.Move(200, 400));

        Assert.Equal(new Bounds(50, 100, 50, 50), context.Marquee);
    }

    [Fact]
    public void EveryTickReplacesTheSelectionWithWhatTheBandIntersects()
    {
        var board = new Board();
        var inside = AddInstance(board, 20, 20);
        var alsoInside = AddInstance(board, 120, 20);
        var outside = AddInstance(board, 500, 500);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(outside.Id);
        var marquee = new MarqueeSelectGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0),
            context
        );

        marquee.Move(PointerEvents.Move(150, 60));

        Assert.Equal(new HashSet<Guid> { inside.Id, alsoInside.Id }, context.SelectedInstanceIds);

        marquee.Move(PointerEvents.Move(60, 60));

        Assert.Equal([inside.Id], context.SelectedInstanceIds);
    }

    [Fact]
    public void ShiftHeldAtPressUnionsTheBandIntoThePressTimeSelection()
    {
        var board = new Board();
        var alreadySelected = AddInstance(board, 500, 500);
        var swept = AddInstance(board, 20, 20);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(alreadySelected.Id);
        context.SelectionSnapshot = new SelectionSnapshot([alreadySelected.Id], []);
        var marquee = new MarqueeSelectGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0, shift: true),
            context
        );

        marquee.Move(PointerEvents.Move(100, 100));

        Assert.Equal(
            new HashSet<Guid> { alreadySelected.Id, swept.Id },
            context.SelectedInstanceIds
        );
    }

    [Fact]
    public void AGroupedMemberIsSweptAsItsGroup()
    {
        var board = new Board();
        var member = AddInstance(board, 20, 20);
        var groupId = Guid.NewGuid();
        var context = new FakeGestureContext(board)
        {
            EffectiveId = id => id == member.Id ? groupId : id,
        };
        var marquee = new MarqueeSelectGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0),
            context
        );

        marquee.Move(PointerEvents.Move(100, 100));

        Assert.Equal([groupId], context.SelectedInstanceIds);
    }

    [Fact]
    public void ReleaseFromActiveHidesTheBandAndKeepsTheSelection()
    {
        var board = new Board();
        var swept = AddInstance(board, 20, 20);
        var context = new FakeGestureContext(board);
        var marquee = new MarqueeSelectGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0),
            context
        );
        marquee.Move(PointerEvents.Move(100, 100));

        marquee.Release(PointerEvents.Release(PointerPress.PrimaryButton, 100, 100));

        Assert.Null(context.Marquee);
        Assert.Equal([swept.Id], context.SelectedInstanceIds);
    }

    [Fact]
    public void AClickOnEmptyCanvasClearsTheSelection()
    {
        var board = new Board();
        var selected = AddInstance(board, 500, 500);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(selected.Id);
        var marquee = new MarqueeSelectGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0),
            context
        );

        marquee.Release(PointerEvents.Release(PointerPress.PrimaryButton, 2, 1));

        Assert.Empty(context.SelectedInstanceIds);
        Assert.Null(context.Marquee);
    }

    [Fact]
    public void AnEdgeIsTakenWhenEveryEndIsOnASweptComponentOrFloatsInsideTheBand()
    {
        var board = new Board();
        var swept = AddInstance(board, 20, 20);
        var unswept = AddInstance(board, 500, 500);
        var interior = new Edge(
            new PortEndpoint(swept.Id, PortId.Right),
            new FloatingEndpoint(90, 90)
        );
        var leaving = new Edge(
            new PortEndpoint(swept.Id, PortId.Right),
            new PortEndpoint(unswept.Id, PortId.Left)
        );
        var dangling = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new FloatingEndpoint(90, 90)
        );
        board.AddEdge(interior);
        board.AddEdge(leaving);
        board.AddEdge(dangling);
        var context = new FakeGestureContext(board);
        var marquee = new MarqueeSelectGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0),
            context
        );

        marquee.Move(PointerEvents.Move(100, 100));

        Assert.Equal([interior.Id], context.SelectedEdgeIds);
    }

    [Fact]
    public void AShrinkingBandDropsTheEdgesItNoLongerCloses()
    {
        var board = new Board();
        var edge = new Edge(new FloatingEndpoint(20, 20), new FloatingEndpoint(80, 20));
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);
        var marquee = new MarqueeSelectGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0),
            context
        );

        marquee.Move(PointerEvents.Move(100, 100));
        Assert.Equal([edge.Id], context.SelectedEdgeIds);

        marquee.Move(PointerEvents.Move(50, 50));
        Assert.Empty(context.SelectedEdgeIds);
    }

    [Fact]
    public void AShiftBandClosesOnlyOverWhatItSweptItself()
    {
        var board = new Board();
        var first = AddInstance(board, 300, 300);
        var second = AddInstance(board, 400, 300);
        board.AddEdge(
            new Edge(
                new PortEndpoint(first.Id, PortId.Right),
                new PortEndpoint(second.Id, PortId.Left)
            )
        );
        var context = new FakeGestureContext(board);
        context.SelectionSnapshot = new SelectionSnapshot([first.Id, second.Id], []);
        var marquee = new MarqueeSelectGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.PrimaryButton, 0, 0, shift: true),
            context
        );

        marquee.Move(PointerEvents.Move(100, 100));

        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, context.SelectedInstanceIds);
        Assert.Empty(context.SelectedEdgeIds);
    }
}
