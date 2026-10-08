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
    public void ADoubleClickOnAnAddressableInstanceAsksForAnInlineEdit()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);

        var move = PressInstance(context, pressed.Id, 10, 10, pressCount: 2);
        move.Release(ReleaseAt(10, 10));

        Assert.Equal([pressed.Id], context.InlineEditRequests);
        Assert.Empty(context.EnteredGroups);
    }

    [Fact]
    public void ADoubleClickOnAMemberOfAGroupNotEnteredEntersItAndSelectsTheMember()
    {
        var board = new Board();
        var member = AddInstance(board, 0, 0);
        var group = new Group([member.Id, AddInstance(board, 100, 0).Id]);
        board.AddGroup(group);
        var context = new FakeGestureContext(board);
        context.EffectiveId = id => context.EnteredGroups.Contains(group.Id) ? id : group.Id;
        context.SelectedInstanceIds.Add(group.Id);

        var move = PressInstance(context, member.Id, 10, 10, pressCount: 2);
        move.Release(ReleaseAt(10, 10));

        Assert.Equal([group.Id], context.EnteredGroups);
        Assert.Equal([member.Id], context.SelectedInstanceIds);
        Assert.Empty(context.InlineEditRequests);
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
    public void ShiftLocksTheAxisThePressAnchoredDeltaMovesLeastAlong()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(70, 25, shift: true));
        Assert.Equal(new Bounds(60, 0, 50, 50), context.Preview[shape.Id]);

        move.Move(PointerEvents.Move(30, 90, shift: true));
        Assert.Equal(new Bounds(0, 80, 50, 50), context.Preview[shape.Id]);
    }

    [Fact]
    public void ShiftIsReadLiveSoReleasingItFreesTheMotionAndPressingItStraightensIt()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(70, 25));
        Assert.Equal(new Bounds(60, 15, 50, 50), context.Preview[shape.Id]);

        move.Move(PointerEvents.Move(70, 25, shift: true));
        Assert.Equal(new Bounds(60, 0, 50, 50), context.Preview[shape.Id]);

        move.Move(PointerEvents.Move(70, 25));
        Assert.Equal(new Bounds(60, 15, 50, 50), context.Preview[shape.Id]);
    }

    [Fact]
    public void TheLockedAxisIsNeverSnappedWhileTheFreeAxisIs()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(3, 7, 50, 50));
        var context = new FakeGestureContext(board) { GridSpacing = 20 };
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(45, 14, shift: true));

        Assert.Equal(new Bounds(40, 7, 50, 50), context.Preview[shape.Id]);
    }

    [Fact]
    public void CtrlSuppressesSnappingAndIsReadLive()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(3, 7, 50, 50));
        var context = new FakeGestureContext(board) { GridSpacing = 20 };
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(45, 24, ctrl: true));
        Assert.Equal(new Bounds(38, 21, 50, 50), context.Preview[shape.Id]);

        move.Move(PointerEvents.Move(45, 24));
        Assert.Equal(new Bounds(40, 20, 50, 50), context.Preview[shape.Id]);
    }

    [Fact]
    public void CtrlWithShiftLeavesTheFreeAxisUnsnappedOnTheLockedLine()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(3, 7, 50, 50));
        var context = new FakeGestureContext(board) { GridSpacing = 20 };
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(45, 14, shift: true, ctrl: true));

        Assert.Equal(new Bounds(38, 7, 50, 50), context.Preview[shape.Id]);
    }

    [Fact]
    public void AReleaseAfterAModifierChangeCommitsTheLastPublishedPosition()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        var context = new FakeGestureContext(board);
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(70, 25, shift: true));
        move.Move(PointerEvents.Move(70, 25));
        move.Release(ReleaseAt(70, 25, shift: true));

        Assert.Equal(new Bounds(60, 15, 50, 50), Assert.Single(context.Commits)[shape.Id]);
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

    [Fact]
    public void ObjectSnappingTakesTheAxisItMatchesAndTheGridFillsTheOther()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        AddInstance(board, new Bounds(203, 300, 80, 50));
        var context = new FakeGestureContext(board) { GridSpacing = 20, ObjectSnapping = true };
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(210, 43));

        Assert.Equal(new Bounds(203, 40, 50, 50), context.Preview[shape.Id]);
        Assert.Equal([new AlignmentGuide(SnapAxis.X, 203)], context.Guides);
    }

    [Fact]
    public void WithObjectSnappingOffOnlyTheGridSnaps()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        AddInstance(board, new Bounds(203, 300, 80, 50));
        var context = new FakeGestureContext(board) { GridSpacing = 20 };
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(210, 43));

        Assert.Equal(new Bounds(200, 40, 50, 50), context.Preview[shape.Id]);
        Assert.Empty(context.Guides);
    }

    [Fact]
    public void CtrlFreesBothSnapsAndIsReadLive()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        AddInstance(board, new Bounds(203, 300, 80, 50));
        var context = new FakeGestureContext(board) { GridSpacing = 20, ObjectSnapping = true };
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(210, 43, ctrl: true));
        Assert.Equal(new Bounds(200, 33, 50, 50), context.Preview[shape.Id]);
        Assert.Empty(context.Guides);

        move.Move(PointerEvents.Move(210, 43));
        Assert.Equal(new Bounds(203, 40, 50, 50), context.Preview[shape.Id]);
    }

    [Fact]
    public void AFastPointerSeesNoObjectSnappingAndSlowingDownBringsItBack()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        AddInstance(board, new Bounds(203, 300, 80, 50));
        var context = new FakeGestureContext(board) { ObjectSnapping = true };
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(210, 43, velocity: ObjectSnap.FastPointerSpeed * 2));
        Assert.Equal(new Bounds(200, 33, 50, 50), context.Preview[shape.Id]);
        Assert.Empty(context.Guides);

        move.Move(PointerEvents.Move(210, 43, velocity: ObjectSnap.FastPointerSpeed / 2));
        Assert.Equal(new Bounds(203, 33, 50, 50), context.Preview[shape.Id]);
    }

    [Fact]
    public void ASnapHoldsUntilThePointerPullsPastTheStickyDistance()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        AddInstance(board, new Bounds(200, 300, 0, 50));
        var context = new FakeGestureContext(board) { ObjectSnapping = true };
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(214, 10));
        Assert.Equal(200, context.Preview[shape.Id].X);

        move.Move(PointerEvents.Move(222, 10));
        Assert.Equal(200, context.Preview[shape.Id].X);

        move.Move(PointerEvents.Move(226, 10));
        Assert.Equal(216, context.Preview[shape.Id].X);
    }

    [Fact]
    public void ToleranceIsInScreenPixelsSoItShrinksOnTheBoardAsTheViewZoomsIn()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        AddInstance(board, new Bounds(106, 200, 2, 50));
        var zoomPan = new ZoomPanTracker();
        var context = new FakeGestureContext(board, zoomPan) { ObjectSnapping = true };
        zoomPan.ZoomAbout(0, 0, 2);
        var move = PressInstance(context, shape.Id, 20, 20);

        move.Move(PointerEvents.Move(220, 20));

        Assert.Equal(100, context.Preview[shape.Id].X);
    }

    [Fact]
    public void TheLockedAxisIsNeverObjectSnapped()
    {
        var board = new Board();
        var shape = AddInstance(board, 0, 0);
        AddInstance(board, new Bounds(400, 4, 50, 50));
        var context = new FakeGestureContext(board) { ObjectSnapping = true };
        var move = PressInstance(context, shape.Id, 10, 10);

        move.Move(PointerEvents.Move(110, 12, shift: true));

        Assert.Equal(new Bounds(100, 0, 50, 50), context.Preview[shape.Id]);
        Assert.Empty(context.Guides);
    }

    [Fact]
    public void AShapeDroppedBesideTwoEquallySpacedOnesLandsAtTheSameGap()
    {
        var board = new Board();
        AddInstance(board, new Bounds(0, 200, 50, 50));
        AddInstance(board, new Bounds(90, 200, 50, 50));
        var third = AddInstance(board, new Bounds(300, 0, 50, 50));
        var context = new FakeGestureContext(board) { ObjectSnapping = true };
        var move = PressInstance(context, third.Id, 310, 10);

        move.Move(PointerEvents.Move(193, 213));
        move.Release(ReleaseAt(193, 213));

        Assert.Equal(new Bounds(180, 200, 50, 50), Assert.Single(context.Commits)[third.Id]);
        Assert.Contains(new SpacingGuide(SnapAxis.X, 140, 180, 225), context.Guides);
        Assert.Contains(new SpacingGuide(SnapAxis.X, 50, 90, 225), context.Guides);
    }

    [Fact]
    public void TheSelectionsOwnMembersAreNeverCandidates()
    {
        var board = new Board();
        var pressed = AddInstance(board, 0, 0);
        var other = AddInstance(board, 100, 3);
        var context = new FakeGestureContext(board) { ObjectSnapping = true };
        context.SelectedInstanceIds.UnionWith([pressed.Id, other.Id]);
        var move = PressInstance(context, pressed.Id, 10, 10);

        move.Move(PointerEvents.Move(12, 12));

        Assert.Equal(new Bounds(2, 2, 50, 50), context.Preview[pressed.Id]);
    }

    [Fact]
    public void AnAxisWhoseMatchLeavesNothingToDrawIsGivenBackToTheGrid()
    {
        var board = new Board();
        AddInstance(board, new Bounds(0, 200, 50, 50));
        AddInstance(board, new Bounds(90, 200, 50, 50));
        var third = AddInstance(board, new Bounds(300, 0, 50, 50));
        var context = new FakeGestureContext(board) { ObjectSnapping = true };
        var move = PressInstance(context, third.Id, 310, 10);

        move.Move(PointerEvents.Move(193, 256));

        Assert.Equal(new Bounds(183, 250, 50, 50), context.Preview[third.Id]);
        Assert.All(context.Guides, guide => Assert.Equal(SnapAxis.Y, ((AlignmentGuide)guide).Axis));
    }
}
