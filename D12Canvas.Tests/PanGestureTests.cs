using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// Pan holds the board point under the press under the pointer, so what the viewport shows depends
// on the whole distance from the press, never an accumulation of per-tick deltas, and a viewport
// change under the press re-anchors to the point then under the pointer. A secondary release that
// never crossed the threshold is the context menu, resolving the selection at that moment; a
// middle click is nothing.
public class PanGestureTests
{
    private const string ComponentTypeKey = "test-props";

    private static ComponentInstance AddInstance(Board board, double x)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, 0, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void ADragPansByTheDistanceFromThePressPointRegardlessOfSkippedFrames()
    {
        var context = new FakeGestureContext(new Board());
        context.ZoomPan.SetPanPosition(100, 50);
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.SecondaryButton, 200, 200),
            context
        );

        pan.Move(PointerEvents.Move(210, 205));
        pan.Move(PointerEvents.Move(260, 180));

        Assert.Equal(GesturePhase.Active, pan.Phase);
        Assert.Equal(160, context.ZoomPan.PanX);
        Assert.Equal(30, context.ZoomPan.PanY);
    }

    [Fact]
    public void ReleasingFromActiveCommitsNothingAndOpensNoMenu()
    {
        var context = new FakeGestureContext(new Board());
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.SecondaryButton, 200, 200),
            context
        );
        pan.Move(PointerEvents.Move(260, 180));

        pan.Release(PointerEvents.Release(PointerPress.SecondaryButton, 260, 180));

        Assert.Empty(context.ContextMenuOpenings);
        Assert.Equal(60, context.ZoomPan.PanX);
    }

    [Fact]
    public void ASecondaryClickOnEmptyCanvasClearsTheSelectionAndOpensTheCanvasMenuAtThePressPoint()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(instance.Id);
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.SecondaryButton, 300, 240),
            context
        );

        pan.Release(PointerEvents.Release(PointerPress.SecondaryButton, 302, 241));

        Assert.Empty(context.SelectedInstanceIds);
        Assert.Equal((300, 240, false), Assert.Single(context.ContextMenuOpenings));
    }

    [Fact]
    public void ASecondaryClickOnAnUnselectedInstanceSelectsItAlone()
    {
        var board = new Board();
        var selected = AddInstance(board, 0);
        var pressed = AddInstance(board, 100);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(selected.Id);
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Instance, PointerPress.SecondaryButton, 10, 10, pressed.Id),
            context
        );

        pan.Release(PointerEvents.Release(PointerPress.SecondaryButton, 10, 10));

        Assert.Equal([pressed.Id], context.SelectedInstanceIds);
        Assert.True(Assert.Single(context.ContextMenuOpenings).PressHitEntity);
    }

    [Fact]
    public void ASecondaryClickInsideTheSelectionPreservesIt()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        var second = AddInstance(board, 100);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.UnionWith([first.Id, second.Id]);
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Instance, PointerPress.SecondaryButton, 10, 10, second.Id),
            context
        );

        pan.Release(PointerEvents.Release(PointerPress.SecondaryButton, 10, 10));

        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, context.SelectedInstanceIds);
        Assert.Single(context.ContextMenuOpenings);
    }

    [Fact]
    public void ASecondaryClickOnAGroupedMemberResolvesToItsGroup()
    {
        var board = new Board();
        var member = AddInstance(board, 0);
        var groupId = Guid.NewGuid();
        var context = new FakeGestureContext(board) { EffectiveId = _ => groupId };
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Instance, PointerPress.SecondaryButton, 10, 10, member.Id),
            context
        );

        pan.Release(PointerEvents.Release(PointerPress.SecondaryButton, 10, 10));

        Assert.Equal([groupId], context.SelectedInstanceIds);
    }

    // The listener resolves an affordance's entity from the nearest marked ancestor, so a press on
    // a resize handle or a port arrives naming the instance it belongs to and selects that.
    [Fact]
    public void ASecondaryClickOnAnAffordanceSelectsTheInstanceItBelongsTo()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var context = new FakeGestureContext(board);
        var pan = new PanGesture(
            PointerEvents.Press(
                HitRole.ResizeHandle,
                PointerPress.SecondaryButton,
                10,
                10,
                instance.Id
            ),
            context
        );

        pan.Release(PointerEvents.Release(PointerPress.SecondaryButton, 10, 10));

        Assert.Equal([instance.Id], context.SelectedInstanceIds);
        Assert.Single(context.ContextMenuOpenings);
    }

    [Fact]
    public void ASecondaryClickOnAnEdgeSelectsTheEdge()
    {
        var edgeId = Guid.NewGuid();
        var context = new FakeGestureContext(new Board());
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Edge, PointerPress.SecondaryButton, 10, 10, edgeId),
            context
        );

        pan.Release(PointerEvents.Release(PointerPress.SecondaryButton, 10, 10));

        Assert.Equal([edgeId], context.SelectedEdgeIds);
        Assert.True(Assert.Single(context.ContextMenuOpenings).PressHitEntity);
    }

    [Fact]
    public void AMiddleClickDoesNothing()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(instance.Id);
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.MiddleButton, 300, 240),
            context
        );

        pan.Release(PointerEvents.Release(PointerPress.MiddleButton, 300, 240));

        Assert.Equal([instance.Id], context.SelectedInstanceIds);
        Assert.Empty(context.ContextMenuOpenings);
        Assert.Equal(0, context.ZoomPan.PanX);
    }

    [Fact]
    public void ACancelledPanIgnoresFurtherMovesAndItsRelease()
    {
        var context = new FakeGestureContext(new Board());
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.SecondaryButton, 200, 200),
            context
        );
        pan.Move(PointerEvents.Move(260, 180));

        pan.MarkCancelled();
        pan.Move(PointerEvents.Move(400, 400));
        pan.Release(PointerEvents.Release(PointerPress.SecondaryButton, 400, 400));

        Assert.Equal(60, context.ZoomPan.PanX);
        Assert.Empty(context.ContextMenuOpenings);
    }

    [Fact]
    public void AViewportChangeUnderAHeldPanReanchorsSoTheTwoMovementsAdd()
    {
        var context = new FakeGestureContext(new Board());
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.MiddleButton, 200, 200),
            context
        );
        pan.Move(PointerEvents.Move(250, 200));

        context.ZoomPan.Pan(-30, 0);
        pan.ViewportMoved(PointerEvents.Move(250, 200));
        pan.Move(PointerEvents.Move(260, 200));

        Assert.Equal(30, context.ZoomPan.PanX);
    }

    [Fact]
    public void AZoomBetweenTicksKeepsTheGrabbedBoardPointUnderThePointer()
    {
        var context = new FakeGestureContext(new Board());
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.MiddleButton, 200, 200),
            context
        );

        context.ZoomPan.Scale = 2;
        pan.Move(PointerEvents.Move(260, 180));

        Assert.Equal((200, 200), context.ToBoardPoint(260, 180));
    }

    [Fact]
    public void AViewportChangePromotesAPointingSecondaryPressSoItsReleaseOpensNoMenu()
    {
        var context = new FakeGestureContext(new Board());
        var pan = new PanGesture(
            PointerEvents.Press(HitRole.Canvas, PointerPress.SecondaryButton, 300, 240),
            context
        );

        var promoted = pan.ViewportMoved(PointerEvents.Move(300, 240));
        pan.Release(PointerEvents.Release(PointerPress.SecondaryButton, 300, 240));

        Assert.True(promoted);
        Assert.Equal(GesturePhase.Active, pan.Phase);
        Assert.Empty(context.ContextMenuOpenings);
    }
}
