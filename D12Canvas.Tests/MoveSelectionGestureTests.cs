using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// A press on board content resolves the selection at once when the pressed entity is not a
// member, and leaves it for the release when it is. Every tick publishes the whole selection
// translated by the board-space distance from the press, snapped as one rigid body by the top-left
// of its bounding box, and an active release commits exactly what was last published.
public class MoveSelectionGestureTests
{
    private const string ComponentTypeKey = "test-props";

    private static ComponentInstance AddInstance(Board board, double x, double y) =>
        AddInstance(board, new Bounds(x, y, 50, 50));

    private static ComponentInstance AddInstance(Board board, Bounds bounds)
    {
        var instance = new ComponentInstance(ComponentTypeKey, new TestProps(), bounds);
        board.AddComponent(instance);
        return instance;
    }

    private static MoveSelectionGesture PressInstance(
        FakeGestureContext context,
        Guid entityId,
        double x,
        double y,
        bool shift = false,
        int pressCount = 1
    ) =>
        Begin(
            new MoveSelectionGesture(
                PointerEvents.Press(
                    HitRole.Instance,
                    PointerPress.PrimaryButton,
                    x,
                    y,
                    entityId,
                    shift,
                    pressCount
                ),
                context
            )
        );

    private static MoveSelectionGesture Begin(MoveSelectionGesture gesture)
    {
        gesture.Begin();
        return gesture;
    }

    private static PointerRelease ReleaseAt(double x, double y, bool shift = false) =>
        PointerEvents.Release(PointerPress.PrimaryButton, x, y) with
        {
            ShiftKey = shift,
        };

    [Fact]
    public void PressingAnUnselectedInstanceSelectsItAtOnce()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var other = AddInstance(board, 200, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(other.Id);

        PressInstance(context, pressed.Id, 10, 10);

        Assert.Equal(new HashSet<Guid> { pressed.Id }, context.SelectedInstanceIds);
    }

    [Fact]
    public void ShiftPressingAnUnselectedInstanceAppendsItAtOnce()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var other = AddInstance(board, 200, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(other.Id);

        PressInstance(context, pressed.Id, 10, 10, shift: true);

        Assert.Equal(new HashSet<Guid> { pressed.Id, other.Id }, context.SelectedInstanceIds);
    }

    [Fact]
    public void PressingAMemberLeavesTheSelectionAloneUntilAClickCollapsesItToThatMember()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var other = AddInstance(board, 200, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.UnionWith([pressed.Id, other.Id]);

        var move = PressInstance(context, pressed.Id, 10, 10);
        Assert.Equal(new HashSet<Guid> { pressed.Id, other.Id }, context.SelectedInstanceIds);

        move.Release(ReleaseAt(10, 10));

        Assert.Equal(new HashSet<Guid> { pressed.Id }, context.SelectedInstanceIds);
        Assert.Empty(context.Commits);
    }

    [Fact]
    public void AShiftClickOnAMemberTogglesItOutAtRelease()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var other = AddInstance(board, 200, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.UnionWith([pressed.Id, other.Id]);

        var move = PressInstance(context, pressed.Id, 10, 10, shift: true);
        Assert.Equal(2, context.SelectedInstanceIds.Count);

        move.Release(ReleaseAt(10, 10, shift: true));

        Assert.Equal(new HashSet<Guid> { other.Id }, context.SelectedInstanceIds);
    }

    [Fact]
    public void AClickOnAnUnselectedInstanceChangesNothingFurtherAtRelease()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var other = AddInstance(board, 200, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(other.Id);

        var move = PressInstance(context, pressed.Id, 10, 10, shift: true);
        move.Release(ReleaseAt(10, 10, shift: true));

        Assert.Equal(new HashSet<Guid> { pressed.Id, other.Id }, context.SelectedInstanceIds);
    }

    [Fact]
    public void ADoublePressOnAMemberLeavesTheSelectionAsItWas()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var other = AddInstance(board, 200, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.UnionWith([pressed.Id, other.Id]);

        var move = PressInstance(context, pressed.Id, 10, 10, shift: true, pressCount: 2);
        move.Release(ReleaseAt(10, 10, shift: true));

        Assert.Equal(new HashSet<Guid> { pressed.Id, other.Id }, context.SelectedInstanceIds);
    }

    [Fact]
    public void ADoubleClickAsksForAnInlineEditOnThePressedEntity()
    {
        var board = new Board();
        var member = AddInstance(board, 0, 0);
        var group = new Group([member.Id, AddInstance(board, 100, 0).Id]);
        board.AddGroup(group);
        var context = new FakeGestureContext(board) { EffectiveId = _ => group.Id };

        var move = PressInstance(context, member.Id, 10, 10, pressCount: 2);
        move.Release(ReleaseAt(10, 10));

        Assert.Equal([member.Id], context.InlineEditRequests);
    }

    [Fact]
    public void ASingleClickOrADoublePressThatDragsAsksForNoInlineEdit()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);

        var click = PressInstance(context, pressed.Id, 10, 10);
        click.Release(ReleaseAt(10, 10));
        var drag = PressInstance(context, pressed.Id, 10, 10, pressCount: 2);
        drag.Move(PointerEvents.Move(60, 10));
        drag.Release(ReleaseAt(60, 10));

        Assert.Empty(context.InlineEditRequests);
    }

    [Fact]
    public void PressingAGroupedMemberSelectsTheGroup()
    {
        var board = new Board();
        var member = AddInstance(board, 0, 0);
        var group = new Group([member.Id, AddInstance(board, 100, 0).Id]);
        board.AddGroup(group);
        var context = new FakeGestureContext(board) { EffectiveId = _ => group.Id };

        PressInstance(context, member.Id, 10, 10);

        Assert.Equal(new HashSet<Guid> { group.Id }, context.SelectedInstanceIds);
    }

    [Fact]
    public void EveryTickPublishesTheWholeSelectionTranslatedByTheBoardSpaceDistanceFromThePress()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var other = AddInstance(board, 200, 100);
        var context = new FakeGestureContext(board);
        context.ZoomPan.Scale = 2;
        context.SelectedInstanceIds.UnionWith([pressed.Id, other.Id]);
        var move = PressInstance(context, pressed.Id, 20, 20);

        move.Move(PointerEvents.Move(30, 26));
        move.Move(PointerEvents.Move(60, 40));

        Assert.Equal(
            new Dictionary<Guid, Bounds>
            {
                [pressed.Id] = new Bounds(20, 10, 50, 50),
                [other.Id] = new Bounds(220, 110, 50, 50),
            },
            context.Preview
        );
        Assert.Equal(new Bounds(0, 0, 50, 50), pressed.Bounds);
    }

    [Fact]
    public void AGroupMovesEveryMember()
    {
        var board = new Board();
        var member = AddInstance(board, 0, 0);
        var otherMember = AddInstance(board, 100, 0);
        var group = new Group([member.Id, otherMember.Id]);
        board.AddGroup(group);
        var context = new FakeGestureContext(board) { EffectiveId = _ => group.Id };
        var move = PressInstance(context, member.Id, 10, 10);

        move.Move(PointerEvents.Move(15, 30));

        Assert.Equal(
            new HashSet<Guid> { member.Id, otherMember.Id },
            context.Preview.Keys.ToHashSet()
        );
        Assert.Equal(new Bounds(105, 20, 50, 50), context.Preview[otherMember.Id]);
    }

    [Fact]
    public void SnappingRoundsTheTopLeftOfTheSelectionsBoundingBoxAndNeverTheSize()
    {
        var board = new Board();
        var pressed = AddInstance(board, new Bounds(3, 7, 45, 33));
        var other = AddInstance(board, new Bounds(53, 47, 45, 33));
        var context = new FakeGestureContext(board) { GridSpacing = 20 };
        context.SelectedInstanceIds.UnionWith([pressed.Id, other.Id]);
        var move = PressInstance(context, pressed.Id, 10, 10);

        move.Move(PointerEvents.Move(38, 22));

        Assert.Equal(new Bounds(40, 20, 45, 33), context.Preview[pressed.Id]);
        Assert.Equal(new Bounds(90, 60, 45, 33), context.Preview[other.Id]);
    }

    [Fact]
    public void AnActiveReleaseCommitsWhatWasLastPublishedAndChangesNoSelection()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var other = AddInstance(board, 200, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.UnionWith([pressed.Id, other.Id]);
        var move = PressInstance(context, pressed.Id, 10, 10);

        move.Move(PointerEvents.Move(40, 50));
        move.Release(ReleaseAt(70, 90));

        var committed = Assert.Single(context.Commits);
        Assert.Equal(new Bounds(30, 40, 50, 50), committed[pressed.Id]);
        Assert.Equal(new HashSet<Guid> { pressed.Id, other.Id }, context.SelectedInstanceIds);
    }

    [Fact]
    public void ThePressOnTheSelectionBoxMovesTheSelectionAndItsClickChangesNothing()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 200, 0);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.UnionWith([first.Id, second.Id]);

        var click = Begin(
            new MoveSelectionGesture(
                PointerEvents.Press(HitRole.SelectionBounds, PointerPress.PrimaryButton, 100, 20),
                context
            )
        );
        click.Release(ReleaseAt(100, 20));
        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, context.SelectedInstanceIds);

        var drag = Begin(
            new MoveSelectionGesture(
                PointerEvents.Press(HitRole.SelectionBounds, PointerPress.PrimaryButton, 100, 20),
                context
            )
        );
        drag.Move(PointerEvents.Move(110, 20));

        Assert.Equal(new Bounds(210, 0, 50, 50), context.Preview[second.Id]);
    }

    [Fact]
    public void TheParticipantsArePublishedAtPressSoTheyAreKnownBeforeTheFirstMove()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);

        PressInstance(context, pressed.Id, 10, 10);

        Assert.Equal(new Bounds(0, 0, 50, 50), context.Preview[pressed.Id]);
    }

    [Fact]
    public void ASelectedEdgesFloatingEndsArePublishedAsMovedEndpointsAndItsAttachedEndsAreNot()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var edge = new Edge(
            new PortEndpoint(pressed.Id, PortId.Right),
            new FloatingEndpoint(300, 300)
        );
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);
        context.SelectedInstanceIds.Add(pressed.Id);
        context.SelectedEdgeIds.Add(edge.Id);

        var move = PressInstance(context, pressed.Id, 10, 10);
        move.Move(PointerEvents.Move(30, 50));

        var moved = Assert.Single(context.MovedEndpoints);
        Assert.Equal(new EdgeEnd(edge.Id, IsSource: false), moved.Key);
        Assert.Equal(new FloatingEndpoint(320, 340), moved.Value);
    }

    [Fact]
    public void AnUnselectedEdgesFloatingEndsStayPut()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        board.AddEdge(new Edge(new FloatingEndpoint(300, 300), new FloatingEndpoint(400, 300)));
        var context = new FakeGestureContext(board);

        var move = PressInstance(context, pressed.Id, 10, 10);
        move.Move(PointerEvents.Move(30, 50));

        Assert.Empty(context.MovedEndpoints);
    }

    [Fact]
    public void PressingAnUnselectedInstanceDropsTheSelectedEdges()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);
        context.SelectedEdgeIds.Add(Guid.NewGuid());

        PressInstance(context, pressed.Id, 10, 10);

        Assert.Empty(context.SelectedEdgeIds);
    }
}
