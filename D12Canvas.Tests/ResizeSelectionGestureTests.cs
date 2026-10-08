using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// A press on a shape's handle or on the selection box's handle resizes the selection's bounding
// box from the board-space distance since the press, with the opposite edge anchored. Under snap
// only the edges the handle moves land on grid lines, never closer than the minimum size allows,
// and members scale exactly inside the box. An active release commits what was last published.
public class ResizeSelectionGestureTests
{
    private const string ComponentTypeKey = "test-props";

    private static ComponentInstance AddInstance(Board board, Bounds bounds)
    {
        var instance = new ComponentInstance(ComponentTypeKey, new TestProps(), bounds);
        board.AddComponent(instance);
        return instance;
    }

    private static ResizeSelectionGesture PressHandle(
        FakeGestureContext context,
        Guid entityId,
        string part,
        double x,
        double y
    ) =>
        Begin(
            new ResizeSelectionGesture(
                PointerEvents.Press(
                    HitRole.ResizeHandle,
                    PointerPress.PrimaryButton,
                    x,
                    y,
                    entityId,
                    part: part
                ),
                context
            )
        );

    private static ResizeSelectionGesture PressSelectionHandle(
        FakeGestureContext context,
        string part,
        double x,
        double y
    ) =>
        Begin(
            new ResizeSelectionGesture(
                PointerEvents.Press(
                    HitRole.SelectionHandle,
                    PointerPress.PrimaryButton,
                    x,
                    y,
                    part: part
                ),
                context
            )
        );

    private static ResizeSelectionGesture Begin(ResizeSelectionGesture gesture)
    {
        gesture.Begin();
        return gesture;
    }

    private static PointerRelease ReleaseAt(double x, double y) =>
        PointerEvents.Release(PointerPress.PrimaryButton, x, y);

    private static FakeGestureContext SelectedOn(Board board, params ComponentInstance[] selected)
    {
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.UnionWith(selected.Select(instance => instance.Id));
        return context;
    }

    [Fact]
    public void TheBottomRightHandleGrowsTheShapeByTheBoardSpaceDistanceWithTheTopLeftAnchored()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(10, 20, 100, 80));
        var context = SelectedOn(board, shape);
        context.ZoomPan.Scale = 2;
        var resize = PressHandle(context, shape.Id, "bottom-right", 220, 200);

        resize.Move(PointerEvents.Move(240, 210));
        resize.Move(PointerEvents.Move(260, 220));

        Assert.Equal(new Bounds(10, 20, 120, 90), context.Preview[shape.Id]);
        Assert.Equal(new Bounds(10, 20, 100, 80), shape.Bounds);
    }

    [Fact]
    public void TheTopLeftHandleKeepsTheBottomRightAnchored()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(100, 100, 100, 80));
        var context = SelectedOn(board, shape);
        var resize = PressHandle(context, shape.Id, "top-left", 100, 100);

        resize.Move(PointerEvents.Move(70, 90));

        Assert.Equal(new Bounds(70, 90, 130, 90), context.Preview[shape.Id]);
    }

    [Fact]
    public void TheMinimumSizeHoldsForASingleShape()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(100, 100, 100, 80));
        var context = SelectedOn(board, shape);
        var resize = PressHandle(context, shape.Id, "top-left", 100, 100);

        resize.Move(PointerEvents.Move(400, 400));

        Assert.Equal(
            new Bounds(150, 130, ResizeMath.DefaultMinWidth, ResizeMath.DefaultMinHeight),
            context.Preview[shape.Id]
        );
    }

    [Fact]
    public void UnderSnapOnlyTheMovingEdgeLandsOnAGridLine()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(3, 7, 95, 63));
        var context = SelectedOn(board, shape);
        context.GridSpacing = 20;
        var resize = PressHandle(context, shape.Id, "right", 98, 40);

        resize.Move(PointerEvents.Move(124, 300));

        Assert.Equal(new Bounds(3, 7, 117, 63), context.Preview[shape.Id]);
    }

    [Fact]
    public void UnderSnapACornerRoundsBothEdgesItMovesAndLeavesTheAnchoredCornerAlone()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(13, 17, 95, 63));
        var context = SelectedOn(board, shape);
        context.GridSpacing = 20;
        var resize = PressHandle(context, shape.Id, "top-left", 13, 17);

        resize.Move(PointerEvents.Move(-12, 4));

        Assert.Equal(new Bounds(-20, 0, 128, 80), context.Preview[shape.Id]);
    }

    [Fact]
    public void UnderSnapTheMovingEdgeTakesTheNearestLineThatKeepsTheMinimumSize()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(3, 0, 100, 60));
        var context = SelectedOn(board, shape);
        context.GridSpacing = 20;
        var resize = PressHandle(context, shape.Id, "right", 103, 30);

        resize.Move(PointerEvents.Move(43, 30));

        Assert.Equal(new Bounds(3, 0, 57, 60), context.Preview[shape.Id]);
    }

    [Fact]
    public void CtrlSuppressesSnappingAndIsReadLive()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(3, 7, 95, 63));
        var context = SelectedOn(board, shape);
        context.GridSpacing = 20;
        var resize = PressHandle(context, shape.Id, "right", 98, 40);

        resize.Move(PointerEvents.Move(124, 40, ctrl: true));
        Assert.Equal(new Bounds(3, 7, 121, 63), context.Preview[shape.Id]);

        resize.Move(PointerEvents.Move(124, 40));
        Assert.Equal(new Bounds(3, 7, 117, 63), context.Preview[shape.Id]);
    }

    [Fact]
    public void ShiftDoesNothingDuringAResize()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(0, 0, 100, 60));
        var context = SelectedOn(board, shape);
        var resize = PressHandle(context, shape.Id, "bottom-right", 100, 60);

        resize.Move(PointerEvents.Move(150, 70, shift: true));

        Assert.Equal(new Bounds(0, 0, 150, 70), context.Preview[shape.Id]);
    }

    [Fact]
    public void ASelectionHandleScalesEveryMemberProportionallyInsideTheBox()
    {
        var board = new Board();
        var first = AddInstance(board, new Bounds(0, 0, 100, 100));
        var second = AddInstance(board, new Bounds(100, 100, 100, 100));
        var context = SelectedOn(board, first, second);
        var resize = PressSelectionHandle(context, "bottom-right", 200, 200);

        resize.Move(PointerEvents.Move(400, 300));

        Assert.Equal(new Bounds(0, 0, 200, 150), context.Preview[first.Id]);
        Assert.Equal(new Bounds(200, 150, 200, 150), context.Preview[second.Id]);
    }

    [Fact]
    public void UnderSnapAMultiSelectionRoundsOnlyTheBoxsMovingEdgeAndNotTheMembers()
    {
        var board = new Board();
        var first = AddInstance(board, new Bounds(0, 0, 70, 100));
        var second = AddInstance(board, new Bounds(70, 0, 130, 100));
        var context = SelectedOn(board, first, second);
        context.GridSpacing = 30;
        var resize = PressSelectionHandle(context, "right", 200, 50);

        resize.Move(PointerEvents.Move(305, 50));

        Assert.Equal(new Bounds(0, 0, 105, 100), context.Preview[first.Id]);
        Assert.Equal(new Bounds(105, 0, 195, 100), context.Preview[second.Id]);
    }

    [Fact]
    public void AMultiSelectionCannotShrinkAnyMemberBelowTheMinimumSize()
    {
        var board = new Board();
        var small = AddInstance(board, new Bounds(0, 0, 100, 100));
        var large = AddInstance(board, new Bounds(100, 0, 300, 100));
        var context = SelectedOn(board, small, large);
        var resize = PressSelectionHandle(context, "right", 400, 50);

        resize.Move(PointerEvents.Move(0, 50));

        Assert.Equal(new Bounds(0, 0, ResizeMath.DefaultMinWidth, 100), context.Preview[small.Id]);
        Assert.Equal(200, context.Preview[large.Id].Right);
    }

    [Fact]
    public void AGroupResizesEveryMember()
    {
        var board = new Board();
        var member = AddInstance(board, new Bounds(0, 0, 100, 100));
        var otherMember = AddInstance(board, new Bounds(100, 0, 100, 100));
        var group = new Group([member.Id, otherMember.Id]);
        board.AddGroup(group);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(group.Id);
        var resize = PressSelectionHandle(context, "bottom", 100, 100);

        resize.Move(PointerEvents.Move(100, 200));

        Assert.Equal(new Bounds(100, 0, 100, 200), context.Preview[otherMember.Id]);
        Assert.Equal(new Bounds(0, 0, 100, 200), context.Preview[member.Id]);
    }

    [Fact]
    public void AnActiveReleaseCommitsWhatWasLastPublishedAndChangesNoSelection()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(0, 0, 100, 100));
        var context = SelectedOn(board, shape);
        var resize = PressHandle(context, shape.Id, "bottom-right", 100, 100);

        resize.Move(PointerEvents.Move(130, 120));
        resize.Release(ReleaseAt(400, 400));

        var committed = Assert.Single(context.Commits);
        Assert.Equal(new Bounds(0, 0, 130, 120), committed[shape.Id]);
        Assert.Equal(new HashSet<Guid> { shape.Id }, context.SelectedInstanceIds);
    }

    [Fact]
    public void AStationaryClickOnAHandleCommitsNothingAndKeepsTheSelection()
    {
        var board = new Board();
        var first = AddInstance(board, new Bounds(0, 0, 100, 100));
        var second = AddInstance(board, new Bounds(200, 0, 100, 100));
        var context = SelectedOn(board, first, second);

        var click = PressSelectionHandle(context, "left", 0, 50);
        click.Release(ReleaseAt(0, 50));

        Assert.Empty(context.Commits);
        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, context.SelectedInstanceIds);
    }

    [Fact]
    public void TheParticipantsArePublishedAtPressSoTheyAreKnownBeforeTheFirstMove()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(0, 0, 100, 100));
        var context = SelectedOn(board, shape);

        PressHandle(context, shape.Id, "bottom", 50, 100);

        Assert.Equal(new Bounds(0, 0, 100, 100), context.Preview[shape.Id]);
    }

    [Fact]
    public void PressingTheHandleOfAShapeOutsideTheSelectionResizesThatShapeAlone()
    {
        var board = new Board();
        var pressed = AddInstance(board, new Bounds(0, 0, 100, 100));
        var other = AddInstance(board, new Bounds(200, 0, 100, 100));
        var context = SelectedOn(board, other);
        var resize = PressHandle(context, pressed.Id, "right", 100, 50);

        resize.Move(PointerEvents.Move(150, 50));

        Assert.Equal(new HashSet<Guid> { pressed.Id }, context.SelectedInstanceIds);
        Assert.Equal(new Bounds(0, 0, 150, 100), Assert.Single(context.Preview).Value);
    }
}
