using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

// A press on a port or a floating endpoint carries one end of an edge: a new edge pulled from a
// port, or the end of an existing edge pinned to it. Each tick publishes one pending line from the
// end that stays put to the pointer, naming the shape under the pointer as the drop target, and
// the release resolves what is under the pointer:
// a port pins the end to it, a shape's body attaches it as an auto endpoint, and nothing at all
// leaves it floating at the release point.
public class DragEdgeEndGestureTests
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

    private static DragEdgeEndGesture Press(
        FakeGestureContext context,
        string role,
        Guid entityId,
        string part,
        double x,
        double y
    )
    {
        var gesture = new DragEdgeEndGesture(
            PointerEvents.Press(role, PointerPress.PrimaryButton, x, y, entityId, part: part),
            context
        );
        gesture.Begin();
        return gesture;
    }

    private static PointerRelease ReleaseAt(double x, double y, params PointerHit[] hits) =>
        PointerEvents.Release(PointerPress.PrimaryButton, x, y) with
        {
            Hits = hits,
        };

    private static PointerHit PortHit(ComponentInstance instance, string part) =>
        new(HitRole.Port, instance.Id, part);

    private static PointerHit BodyHit(ComponentInstance instance) =>
        new(HitRole.Instance, instance.Id, null);

    [Fact]
    public void ADragFromABarePortPublishesALineFromThatPortToThePointer()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        Assert.Null(context.PendingEdge);

        gesture.Move(PointerEvents.Move(180, 130));

        Assert.Equal(
            new PendingEdge(null, false, new PortEndpoint(source.Id, PortId.Right), (180, 130)),
            context.PendingEdge
        );
    }

    [Fact]
    public void TheLineEndsAtTheBoardPointUnderThePointerWhenZoomed()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var zoomPan = new ZoomPanTracker();
        zoomPan.SetPanPosition(10, 20);
        var context = new FakeGestureContext(board, zoomPan);

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 160, 145);
        gesture.Move(PointerEvents.Move(210, 220));

        Assert.Equal((200, 200), context.PendingEdge!.Point);
    }

    [Fact]
    public void ReleasingOverAnotherShapesPortCreatesAnEdgePinnedToBothPorts()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        gesture.Move(PointerEvents.Move(250, 125));
        gesture.Release(ReleaseAt(250, 125, PortHit(target, "Left"), BodyHit(target)));

        var (from, to) = Assert.Single(context.AddedEdges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), from);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), to);
    }

    [Fact]
    public void ReleasingOverACustomPortPinsToIt()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var custom = new PortDef(0.5, 1.0);
        target.CustomPorts.Add(custom);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        gesture.Move(PointerEvents.Move(275, 150));
        gesture.Release(ReleaseAt(275, 150, PortHit(target, custom.Id.ToString())));

        Assert.Equal(
            new CustomPortEndpoint(target.Id, custom.Id),
            Assert.Single(context.AddedEdges).Target
        );
    }

    public static TheoryData<string, string?, string> DropZones =>
        new()
        {
            { HitRole.Port, "Left", "pinned" },
            { HitRole.Instance, null, "auto" },
            { HitRole.AuthorContent, null, "auto" },
            { "none", null, "floating" },
        };

    [Theory]
    [MemberData(nameof(DropZones))]
    public void WhatLiesUnderTheReleaseDecidesHowTheEndAttaches(
        string role,
        string? part,
        string expected
    )
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var context = new FakeGestureContext(board);
        PointerHit[] hits = role == "none" ? [] : [new PointerHit(role, target.Id, part)];

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        gesture.Move(PointerEvents.Move(270, 120));
        gesture.Release(ReleaseAt(270, 120, hits));

        IEdgeEndpoint expectedEnd = expected switch
        {
            "pinned" => new PortEndpoint(target.Id, PortId.Left),
            "auto" => new AutoPortEndpoint(target.Id),
            _ => new FloatingEndpoint(270, 120),
        };
        Assert.Equal(expectedEnd, Assert.Single(context.AddedEdges).Target);
    }

    [Fact]
    public void ReleasingOnTheStartingShapesBodyOrAnotherOfItsPortsCreatesNothing()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var context = new FakeGestureContext(board);

        var onBody = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        onBody.Move(PointerEvents.Move(120, 120));
        onBody.Release(ReleaseAt(120, 120, BodyHit(source)));

        var onOtherPort = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        onOtherPort.Move(PointerEvents.Move(100, 125));
        onOtherPort.Release(ReleaseAt(100, 125, PortHit(source, "Left"), BodyHit(source)));

        Assert.Empty(context.AddedEdges);
    }

    [Fact]
    public void AnEdgeLabelOverAShapeIsLookedThroughToTheShape()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var labelled = new Edge(new FloatingEndpoint(0, 0), new FloatingEndpoint(500, 500));
        board.AddEdge(labelled);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        gesture.Move(PointerEvents.Move(270, 120));
        gesture.Release(
            ReleaseAt(
                270,
                120,
                new PointerHit(HitRole.AuthorContent, labelled.Id, null),
                new PointerHit(HitRole.EdgeLabel, labelled.Id, null),
                BodyHit(target)
            )
        );

        Assert.Equal(new AutoPortEndpoint(target.Id), Assert.Single(context.AddedEdges).Target);
    }

    // A port beneath the body that is on top is not what the user sees, and a resize handle or the
    // selection box over a port is chrome the drop looks straight through.
    [Fact]
    public void TheTopmostPortOrBodyDecidesAndChromeAboveItIsIgnored()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var below = AddInstance(board, 250, 100);
        var above = AddInstance(board, 250, 100);
        var context = new FakeGestureContext(board);

        var throughChrome = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        throughChrome.Move(PointerEvents.Move(250, 125));
        throughChrome.Release(
            ReleaseAt(
                250,
                125,
                new PointerHit(HitRole.SelectionBounds, null, null),
                new PointerHit(HitRole.ResizeHandle, above.Id, "left"),
                PortHit(above, "Left"),
                BodyHit(above)
            )
        );

        var buried = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        buried.Move(PointerEvents.Move(250, 125));
        buried.Release(ReleaseAt(250, 125, BodyHit(above), PortHit(below, "Left")));

        Assert.Equal(new PortEndpoint(above.Id, PortId.Left), context.AddedEdges[0].Target);
        Assert.Equal(new AutoPortEndpoint(above.Id), context.AddedEdges[1].Target);
    }

    [Fact]
    public void ReleasingBackOnTheStartingPortCreatesNothing()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        gesture.Move(PointerEvents.Move(200, 125));
        gesture.Move(PointerEvents.Move(150, 125));
        gesture.Release(ReleaseAt(150, 125, PortHit(source, "Right"), BodyHit(source)));

        Assert.Empty(context.AddedEdges);
    }

    [Fact]
    public void AClickOnAPortChangesNothing()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        gesture.Release(ReleaseAt(150, 125, PortHit(source, "Right")));

        Assert.Empty(context.AddedEdges);
        Assert.Empty(context.EndpointChanges);
        Assert.Null(context.PendingEdge);
    }

    [Fact]
    public void ADragFromAPortThatAnchorsAnEdgeCarriesThatEdgesEnd()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, target.Id, "Left", 250, 125);
        gesture.Move(PointerEvents.Move(300, 300));

        Assert.Equal(
            new PendingEdge(edge.Id, false, new PortEndpoint(source.Id, PortId.Right), (300, 300)),
            context.PendingEdge
        );

        gesture.Release(ReleaseAt(300, 300));

        Assert.Empty(context.AddedEdges);
        Assert.Equal(
            (edge.Id, false, (IEdgeEndpoint)new FloatingEndpoint(300, 300)),
            Assert.Single(context.EndpointChanges)
        );
    }

    [Fact]
    public void ADragFromAFloatingEndpointCarriesItAndCanPinItToAPort()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new FloatingEndpoint(200, 300)
        );
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.EdgeEndpoint, edge.Id, "target", 200, 300);
        gesture.Move(PointerEvents.Move(250, 125));
        gesture.Release(ReleaseAt(250, 125, PortHit(target, "Left")));

        Assert.Equal(
            (edge.Id, false, (IEdgeEndpoint)new PortEndpoint(target.Id, PortId.Left)),
            Assert.Single(context.EndpointChanges)
        );
    }

    [Fact]
    public void ADraggedSourceEndDrawsItsLineFromTheTarget()
    {
        var board = new Board();
        var target = AddInstance(board, 250, 100);
        var edge = new Edge(new FloatingEndpoint(50, 50), new PortEndpoint(target.Id, PortId.Left));
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.EdgeEndpoint, edge.Id, "source", 50, 50);
        gesture.Move(PointerEvents.Move(70, 60));

        Assert.Equal(
            new PendingEdge(edge.Id, true, new PortEndpoint(target.Id, PortId.Left), (70, 60)),
            context.PendingEdge
        );
    }

    [Fact]
    public void DroppingAnEndOntoTheOtherEndsPortOrBackWhereItWasChangesNothing()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);

        var ontoOther = Press(context, HitRole.Port, target.Id, "Left", 250, 125);
        ontoOther.Move(PointerEvents.Move(150, 125));
        ontoOther.Release(ReleaseAt(150, 125, PortHit(source, "Right")));

        var backAgain = Press(context, HitRole.Port, target.Id, "Left", 250, 125);
        backAgain.Move(PointerEvents.Move(300, 125));
        backAgain.Move(PointerEvents.Move(250, 125));
        backAgain.Release(ReleaseAt(250, 125, PortHit(target, "Left")));

        Assert.Empty(context.EndpointChanges);
    }

    [Fact]
    public void DroppingACarriedEndOnAShapesBodyMakesItAnAutoEndpoint()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, target.Id, "Left", 250, 125);
        gesture.Move(PointerEvents.Move(280, 140));
        gesture.Release(ReleaseAt(280, 140, BodyHit(target)));

        Assert.Equal(
            (edge.Id, false, (IEdgeEndpoint)new AutoPortEndpoint(target.Id)),
            Assert.Single(context.EndpointChanges)
        );
    }

    [Fact]
    public void DroppingACarriedEndOnTheOtherEndsShapeChangesNothing()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var edge = new Edge(new AutoPortEndpoint(source.Id), new FloatingEndpoint(400, 125));
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.EdgeEndpoint, edge.Id, "target", 400, 125);
        gesture.Move(PointerEvents.Move(120, 120));
        gesture.Release(ReleaseAt(120, 120, BodyHit(source)));

        Assert.Empty(context.EndpointChanges);
        Assert.Empty(context.AddedEdges);
    }

    // An auto end has no port of its own, and the side it sits on moves with the other end, so a
    // press on that port pulls a new edge rather than carrying an end the user cannot see is there.
    [Fact]
    public void ADragFromThePortAnAutoEndResolvesToPullsANewEdge()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new AutoPortEndpoint(target.Id)
        );
        board.AddEdge(edge);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, target.Id, "Left", 250, 125);
        gesture.Move(PointerEvents.Move(300, 300));
        gesture.Release(ReleaseAt(300, 300));

        Assert.Empty(context.EndpointChanges);
        Assert.Equal(
            (new PortEndpoint(target.Id, PortId.Left), new FloatingEndpoint(300, 300)),
            Assert.Single(context.AddedEdges)
        );
    }

    [Fact]
    public void TheShapeUnderThePointerIsTheDropTarget()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        gesture.Move(MoveOver(270, 120, BodyHit(target)));
        Assert.Equal(target.Id, context.PendingEdge!.DropTargetId);

        gesture.Move(MoveOver(275, 100, PortHit(target, "Top"), BodyHit(target)));
        Assert.Equal(target.Id, context.PendingEdge!.DropTargetId);

        gesture.Move(MoveOver(400, 400));
        Assert.Null(context.PendingEdge!.DropTargetId);
    }

    [Fact]
    public void TheShapeTheOtherEndIsOnIsNeverTheDropTarget()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var context = new FakeGestureContext(board);

        var gesture = Press(context, HitRole.Port, source.Id, "Right", 150, 125);
        gesture.Move(MoveOver(120, 120, BodyHit(source)));

        Assert.Null(context.PendingEdge!.DropTargetId);
    }

    private static PointerMove MoveOver(double x, double y, params PointerHit[] hits) =>
        PointerEvents.Move(x, y) with
        {
            Hits = hits,
        };
}
