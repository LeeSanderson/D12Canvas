using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class BoardTests
{
    [Fact]
    public void AddedComponentIsRetrievableByItsId()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );

        board.AddComponent(instance);

        Assert.Same(instance, board.GetComponent(instance.Id));
    }

    [Fact]
    public void GetComponentForAnUnknownIdReturnsNull()
    {
        var board = new Board();

        Assert.Null(board.GetComponent(Guid.NewGuid()));
    }

    [Fact]
    public void RemovedComponentIsNoLongerRetrievable()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );
        board.AddComponent(instance);

        board.RemoveComponent(instance.Id);

        Assert.Null(board.GetComponent(instance.Id));
    }

    [Fact]
    public void RemovingAnUnknownIdIsANoOp()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );
        board.AddComponent(instance);

        board.RemoveComponent(Guid.NewGuid());

        Assert.Same(instance, board.GetComponent(instance.Id));
    }

    [Fact]
    public void ComponentsExposesEveryAddedInstance()
    {
        var board = new Board();
        var first = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );
        var second = new ComponentInstance(
            "rectangle",
            new TestProps(),
            new Bounds(10, 10, 50, 50)
        );

        board.AddComponent(first);
        board.AddComponent(second);

        Assert.Equal(new[] { first, second }, board.Components, ReferenceEqualityComparer.Instance);
    }

    [Fact]
    public void GetVisibleOnAnEmptyBoardReturnsNoInstances()
    {
        var board = new Board();

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100));

        Assert.Empty(visible);
    }

    [Fact]
    public void GetVisibleReturnsAnInstanceFullyInsideTheViewport()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(10, 10, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100));

        Assert.Same(instance, Assert.Single(visible));
    }

    [Fact]
    public void GetVisibleExcludesAnInstanceFullyOutsideTheViewport()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(200, 200, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100));

        Assert.Empty(visible);
    }

    [Fact]
    public void GetVisibleIncludesAnInstanceThatOnlyTouchesTheViewportsRightEdge()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(100, 10, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100));

        Assert.Same(instance, Assert.Single(visible));
    }

    [Fact]
    public void GetVisibleIncludesAnInstanceThatOnlyTouchesTheViewportsBottomEdge()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(10, 100, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100));

        Assert.Same(instance, Assert.Single(visible));
    }

    [Fact]
    public void GetVisibleIncludesAnInstanceThatOnlyTouchesTheViewportsLeftEdge()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(-20, 10, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100));

        Assert.Same(instance, Assert.Single(visible));
    }

    [Fact]
    public void GetVisibleIncludesAnInstanceThatOnlyTouchesTheViewportsTopEdge()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(10, -20, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100));

        Assert.Same(instance, Assert.Single(visible));
    }

    [Fact]
    public void GetVisibleIncludesAnInstanceThatOnlyTouchesTheExpandedViewportsEdge()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(150, 10, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100), overscan: 50);

        Assert.Same(instance, Assert.Single(visible));
    }

    [Fact]
    public void GetVisibleReturnsExactlyTheIntersectingInstancesFromAMixedBoard()
    {
        var board = new Board();
        var inside = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(10, 10, 20, 20)
        );
        var touchingEdge = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(100, 10, 20, 20)
        );
        var outside = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(200, 200, 20, 20)
        );
        board.AddComponent(inside);
        board.AddComponent(touchingEdge);
        board.AddComponent(outside);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100));

        Assert.Equal(new[] { inside, touchingEdge }, visible, ReferenceEqualityComparer.Instance);
    }

    [Fact]
    public void GetVisibleDefaultsToNoOverscan()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(120, 10, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100));

        Assert.Empty(visible);
    }

    [Fact]
    public void GetVisibleIncludesAnInstanceOutsideTheViewportButInsideTheOverscanMargin()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(120, 10, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100), overscan: 50);

        Assert.Same(instance, Assert.Single(visible));
    }

    [Fact]
    public void GetVisibleExcludesAnInstanceOutsideTheViewportAndTheOverscanMargin()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(1000, 1000, 20, 20)
        );
        board.AddComponent(instance);

        var visible = board.GetVisible(new Bounds(0, 0, 100, 100), overscan: 50);

        Assert.Empty(visible);
    }

    // Ticket 44: Group/ungroup lifecycle - Board stores Group entities alongside components,
    // resolves a group's bounds from its members on demand (never stored), and can find the
    // outermost group containing a given member id.
    [Fact]
    public void AddedGroupIsRetrievableByItsId()
    {
        var board = new Board();
        var group = new Group(new[] { Guid.NewGuid(), Guid.NewGuid() });

        board.AddGroup(group);

        Assert.Same(group, board.GetGroup(group.Id));
    }

    [Fact]
    public void GetGroupForAnUnknownIdReturnsNull()
    {
        var board = new Board();

        Assert.Null(board.GetGroup(Guid.NewGuid()));
    }

    [Fact]
    public void RemovedGroupIsNoLongerRetrievable()
    {
        var board = new Board();
        var group = new Group(new[] { Guid.NewGuid(), Guid.NewGuid() });
        board.AddGroup(group);

        board.RemoveGroup(group.Id);

        Assert.Null(board.GetGroup(group.Id));
    }

    [Fact]
    public void GroupsExposesEveryAddedGroup()
    {
        var board = new Board();
        var first = new Group(new[] { Guid.NewGuid(), Guid.NewGuid() });
        var second = new Group(new[] { Guid.NewGuid(), Guid.NewGuid() });

        board.AddGroup(first);
        board.AddGroup(second);

        Assert.Equal(new[] { first, second }, board.Groups, ReferenceEqualityComparer.Instance);
    }

    [Fact]
    public void GetBoundsComputesTheUnionOfMemberComponentBounds()
    {
        var board = new Board();
        var first = new ComponentInstance("sticky-note", new TestProps(), new Bounds(0, 0, 50, 50));
        var second = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(300, 100, 50, 50)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        var group = new Group(new[] { first.Id, second.Id });

        var bounds = board.GetBounds(group);

        Assert.Equal(new Bounds(0, 0, 350, 150), bounds);
    }

    [Fact]
    public void GetBoundsResolvesNestedGroupsRecursively()
    {
        var board = new Board();
        var first = new ComponentInstance("sticky-note", new TestProps(), new Bounds(0, 0, 50, 50));
        var second = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(300, 100, 50, 50)
        );
        var third = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(-20, -20, 10, 10)
        );
        board.AddComponent(first);
        board.AddComponent(second);
        board.AddComponent(third);
        var inner = new Group(new[] { first.Id, second.Id });
        board.AddGroup(inner);
        var outer = new Group(new[] { inner.Id, third.Id });

        var bounds = board.GetBounds(outer);

        Assert.Equal(new Bounds(-20, -20, 370, 170), bounds);
    }

    [Fact]
    public void GetBoundsSkipsAMemberIdThatNoLongerResolvesToAnything()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(10, 10, 20, 20)
        );
        board.AddComponent(instance);
        var group = new Group(new[] { instance.Id, Guid.NewGuid() });

        var bounds = board.GetBounds(group);

        Assert.Equal(new Bounds(10, 10, 20, 20), bounds);
    }

    [Fact]
    public void GetBoundsForAGroupWithNoResolvableMembersReturnsNull()
    {
        var board = new Board();
        var group = new Group(new[] { Guid.NewGuid() });

        Assert.Null(board.GetBounds(group));
    }

    [Fact]
    public void FindContainingGroupReturnsTheGroupDirectlyContainingAMember()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        board.AddComponent(instance);
        var group = new Group(new[] { instance.Id });
        board.AddGroup(group);

        Assert.Same(group, board.FindContainingGroup(instance.Id));
    }

    [Fact]
    public void FindContainingGroupWalksUpThroughNesting()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        board.AddComponent(instance);
        var inner = new Group(new[] { instance.Id });
        board.AddGroup(inner);
        var outer = new Group(new[] { inner.Id });
        board.AddGroup(outer);

        Assert.Same(outer, board.FindContainingGroup(instance.Id));
        Assert.Same(outer, board.FindContainingGroup(inner.Id));
    }

    [Fact]
    public void FindContainingGroupReturnsNullWhenNotGrouped()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        board.AddComponent(instance);

        Assert.Null(board.FindContainingGroup(instance.Id));
    }

    // Ticket 48: Board.Edges (ADR 0005) - mirrors the Groups add/remove/get coverage above.
    [Fact]
    public void AddedEdgeIsRetrievableByItsId()
    {
        var board = new Board();
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );

        board.AddEdge(edge);

        Assert.Same(edge, board.GetEdge(edge.Id));
    }

    [Fact]
    public void GetEdgeForAnUnknownIdReturnsNull()
    {
        var board = new Board();

        Assert.Null(board.GetEdge(Guid.NewGuid()));
    }

    [Fact]
    public void RemovedEdgeIsNoLongerRetrievable()
    {
        var board = new Board();
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        board.AddEdge(edge);

        board.RemoveEdge(edge.Id);

        Assert.Null(board.GetEdge(edge.Id));
    }

    [Fact]
    public void EdgesExposesEveryAddedEdge()
    {
        var board = new Board();
        var first = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        var second = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Top),
            new PortEndpoint(Guid.NewGuid(), PortId.Bottom)
        );

        board.AddEdge(first);
        board.AddEdge(second);

        Assert.Equal(new[] { first, second }, board.Edges, ReferenceEqualityComparer.Instance);
    }

    // Ticket 48: ResolveEndpoint resolves an attached endpoint's live board point from its
    // referenced instance's current Bounds - what lets an edge track move/resize for free.
    [Fact]
    public void ResolveEndpointReturnsThePortsCurrentBoardPoint()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(100, 100, 40, 20)
        );
        board.AddComponent(instance);

        var point = board.ResolveEndpoint(new PortEndpoint(instance.Id, PortId.Right));

        Assert.Equal((140.0, 110.0), point);
    }

    [Fact]
    public void ResolveEndpointTracksTheInstanceAfterItMoves()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(100, 100, 40, 20)
        );
        board.AddComponent(instance);
        var endpoint = new PortEndpoint(instance.Id, PortId.Right);
        var before = board.ResolveEndpoint(endpoint);

        instance.Bounds = new Bounds(200, 300, 40, 20);
        var after = board.ResolveEndpoint(endpoint);

        Assert.Equal((140.0, 110.0), before);
        Assert.Equal((240.0, 310.0), after);
    }

    [Fact]
    public void ResolveEndpointTracksTheInstanceAfterItResizes()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 40, 20)
        );
        board.AddComponent(instance);
        var endpoint = new PortEndpoint(instance.Id, PortId.Bottom);

        instance.Bounds = new Bounds(0, 0, 80, 100);
        var point = board.ResolveEndpoint(endpoint);

        Assert.Equal((40.0, 100.0), point);
    }

    [Fact]
    public void ResolveEndpointForAMissingComponentReturnsNull()
    {
        var board = new Board();

        var point = board.ResolveEndpoint(new PortEndpoint(Guid.NewGuid(), PortId.Top));

        Assert.Null(point);
    }

    // Ticket 48: FindPortNear - the connector-drag drop hit-test. ADR 0005 settled on discrete
    // named ports, so this only resolves within a tolerance of an instance's actual port points.
    [Fact]
    public void FindPortNearReturnsThePortWithinTolerance()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(100, 100, 40, 20)
        );
        board.AddComponent(instance);

        var found = board.FindPortNear((141, 111), tolerance: 5);

        Assert.Equal(new PortEndpoint(instance.Id, PortId.Right), found);
    }

    [Fact]
    public void FindPortNearReturnsNullWhenNothingIsWithinTolerance()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(100, 100, 40, 20)
        );
        board.AddComponent(instance);

        var found = board.FindPortNear((141, 111), tolerance: 0.5);

        Assert.Null(found);
    }

    [Fact]
    public void FindPortNearPicksTheClosestPortAcrossMultipleInstances()
    {
        var board = new Board();
        var near = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(100, 100, 40, 20)
        );
        var far = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(300, 300, 40, 20)
        );
        board.AddComponent(near);
        board.AddComponent(far);

        // (140, 110) is exactly "near"'s right port and far away from every port on "far".
        var found = board.FindPortNear((140, 110), tolerance: 50);

        Assert.Equal(new PortEndpoint(near.Id, PortId.Right), found);
    }

    [Fact]
    public void FindPortNearOnAnEmptyBoardReturnsNull()
    {
        var board = new Board();

        Assert.Null(board.FindPortNear((0, 0), tolerance: 1000));
    }
}
