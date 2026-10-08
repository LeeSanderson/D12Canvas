using D12Canvas.Model;
using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

// What a copy carries and how a fragment is placed: selected instances closed over the edges
// between them, every selected edge with any end it cannot keep floating, groups recursively and
// the assets the copy refers to; every entity id new on the way out, asset ids kept.
public class BoardFragmentTests
{
    private const string PlainKey = "test-props";

    private static readonly ComponentRegistry Registry = BuildRegistry();

    private static ComponentRegistry BuildRegistry()
    {
        var options = new D12CanvasOptions();
        AssetTestComponent.Register(options);
        options.RegisterComponent<TestComponentDouble, TestProps>(
            PlainKey,
            builder =>
            {
                builder.DisplayName = "Test Props";
                builder.AccessibleName = "Test props component";
                builder.DefaultProps = new TestProps();
            }
        );
        return (ComponentRegistry)options.Registry;
    }

    private static ComponentInstance AddShape(
        Board board,
        double x,
        double y = 0,
        int zIndex = 0,
        IReadOnlyList<PortDef>? customPorts = null
    )
    {
        var instance = new ComponentInstance(
            PlainKey,
            new TestProps(),
            new Bounds(x, y, 100, 50),
            zIndex,
            customPorts: customPorts
        );
        board.AddComponent(instance);
        return instance;
    }

    private static Edge Connect(Board board, ComponentInstance from, ComponentInstance to)
    {
        var edge = new Edge(
            new PortEndpoint(from.Id, PortId.Right),
            new PortEndpoint(to.Id, PortId.Left)
        );
        board.AddEdge(edge);
        return edge;
    }

    private static Board Copy(
        Board board,
        IEnumerable<Guid> ids,
        IEnumerable<Guid>? edgeIds = null
    ) => BoardFragment.Of(board, ids, edgeIds ?? [], Registry);

    [Fact]
    public void TwoConnectedShapesCopyWithTheEdgeBetweenThem()
    {
        var board = new Board();
        var left = AddShape(board, 0);
        var right = AddShape(board, 200);
        var edge = Connect(board, left, right);

        var fragment = Copy(board, [left.Id, right.Id]);

        Assert.Equal(2, fragment.Components.Count);
        var copied = Assert.Single(fragment.Edges);
        Assert.Equal(edge.Source, copied.Source);
        Assert.Equal(edge.Target, copied.Target);
    }

    [Fact]
    public void AnEdgeNobodySelectedToAShapeOutsideTheCopyStaysBehind()
    {
        var board = new Board();
        var copied = AddShape(board, 0);
        var outside = AddShape(board, 200);
        Connect(board, copied, outside);

        var fragment = Copy(board, [copied.Id]);

        Assert.Single(fragment.Components);
        Assert.Empty(fragment.Edges);
    }

    [Fact]
    public void ASelectedEdgeToAShapeOutsideTheCopyFloatsWhereThatEndIsNow()
    {
        var board = new Board();
        var copied = AddShape(board, 0);
        var outside = AddShape(board, 200, 40);
        var edge = Connect(board, copied, outside);

        var fragment = Copy(board, [copied.Id], [edge.Id]);

        var carried = Assert.Single(fragment.Edges);
        Assert.Equal(new PortEndpoint(copied.Id, PortId.Right), carried.Source);
        Assert.Equal(new FloatingEndpoint(200, 65), carried.Target);
    }

    [Fact]
    public void ALoneSelectedEdgeCopiesAsAFloatingLine()
    {
        var board = new Board();
        var left = AddShape(board, 0);
        var right = AddShape(board, 200);
        var edge = Connect(board, left, right);

        var fragment = Copy(board, [], [edge.Id]);

        Assert.Empty(fragment.Components);
        var carried = Assert.Single(fragment.Edges);
        Assert.Equal(new FloatingEndpoint(100, 25), carried.Source);
        Assert.Equal(new FloatingEndpoint(200, 25), carried.Target);
    }

    [Fact]
    public void ASelectedGroupCarriesItsMembersAndNestedGroupsRecursively()
    {
        var board = new Board();
        var first = AddShape(board, 0);
        var second = AddShape(board, 200);
        var third = AddShape(board, 400);
        var inner = new Group([first.Id, second.Id]);
        var outer = new Group([inner.Id, third.Id]);
        board.AddGroup(inner);
        board.AddGroup(outer);

        var fragment = Copy(board, [outer.Id]);

        Assert.Equal(3, fragment.Components.Count);
        Assert.Equal([inner.Id, third.Id], fragment.GetGroup(outer.Id)!.MemberIds);
        Assert.Equal([first.Id, second.Id], fragment.GetGroup(inner.Id)!.MemberIds);
    }

    [Fact]
    public void ACopyNeverSharesAnInstanceWithTheBoard()
    {
        var board = new Board();
        var original = AddShape(board, 0);

        var fragment = Copy(board, [original.Id]);
        fragment.GetComponent(original.Id)!.Bounds = new Bounds(500, 500, 1, 1);

        Assert.Equal(new Bounds(0, 0, 100, 50), original.Bounds);
    }

    [Fact]
    public void AReferencedAssetTravelsAndAnUnreferencedOneDoesNot()
    {
        var board = new Board();
        var reference = board.AddAsset(AssetTestComponent.PngBytes, "image/png");
        board.AddAsset([1, 2, 3], "image/png");
        var shown = new ComponentInstance(
            AssetTestComponent.Key,
            new AssetTestProps(reference),
            new Bounds(0, 0, 10, 10)
        );
        board.AddComponent(shown);

        var fragment = Copy(board, [shown.Id]);

        var asset = Assert.Single(fragment.Assets);
        Assert.Equal(Asset.IdFor(AssetTestComponent.PngBytes), asset.Id);
    }

    [Fact]
    public void FreshIdsReplaceEveryEntityIdAndRewireEveryReference()
    {
        var board = new Board();
        var port = new PortDef(0.5, 0);
        var first = AddShape(board, 0, customPorts: [port]);
        var second = AddShape(board, 200);
        var third = AddShape(board, 400);
        var inner = new Group([first.Id, second.Id]);
        var outer = new Group([inner.Id, third.Id]);
        board.AddGroup(inner);
        board.AddGroup(outer);
        board.AddEdge(
            new Edge(
                new CustomPortEndpoint(first.Id, port.Id),
                new PortEndpoint(second.Id, PortId.Left),
                label: new ComponentInstance(PlainKey, new TestProps(), new Bounds(0, 0, 40, 20))
            )
        );
        board.AddEdge(new Edge(new AutoPortEndpoint(second.Id), new AutoPortEndpoint(third.Id)));
        var originalIds = board
            .Components.Select(c => c.Id)
            .Concat(board.Groups.Select(g => g.Id))
            .Concat(board.Edges.Select(e => e.Id))
            .Concat(board.Edges.Select(e => e.Label?.Id).OfType<Guid>())
            .Append(port.Id)
            .ToHashSet();

        var fresh = BoardFragment.WithFreshIds(Copy(board, [outer.Id]));

        var newIds = fresh
            .Components.Select(c => c.Id)
            .Concat(fresh.Groups.Select(g => g.Id))
            .Concat(fresh.Edges.Select(e => e.Id))
            .Concat(fresh.Edges.Select(e => e.Label?.Id).OfType<Guid>())
            .Concat(fresh.Components.SelectMany(c => c.CustomPorts).Select(p => p.Id))
            .ToList();
        Assert.Equal(9, newIds.Count);
        Assert.Empty(newIds.Intersect(originalIds));

        var freshOuter = Assert.Single(
            fresh.Groups,
            group => fresh.GetGroup(group.MemberIds[0]) is not null
        );
        var freshInner = fresh.GetGroup(freshOuter.MemberIds[0])!;
        Assert.All(
            freshInner.MemberIds.Append(freshOuter.MemberIds[1]),
            id => Assert.NotNull(fresh.GetComponent(id))
        );

        var customEnd = fresh.Edges.Select(e => e.Source).OfType<CustomPortEndpoint>().Single();
        var owner = fresh.GetComponent(customEnd.ComponentId)!;
        Assert.Equal(Assert.Single(owner.CustomPorts).Id, customEnd.PortId);
        Assert.All(
            fresh.Edges,
            edge =>
            {
                Assert.NotNull(fresh.ResolveEnd(edge, isSource: true));
                Assert.NotNull(fresh.ResolveEnd(edge, isSource: false));
            }
        );
    }

    [Fact]
    public void FreshIdsKeepAssetIds()
    {
        var board = new Board();
        var reference = board.AddAsset(AssetTestComponent.PngBytes, "image/png");
        var shown = new ComponentInstance(
            AssetTestComponent.Key,
            new AssetTestProps(reference),
            new Bounds(0, 0, 10, 10)
        );
        board.AddComponent(shown);

        var fresh = BoardFragment.WithFreshIds(Copy(board, [shown.Id]));

        Assert.Equal(reference, ((AssetTestProps)fresh.Components.Single().Props).Src);
        Assert.Equal(Asset.IdFor(AssetTestComponent.PngBytes), fresh.Assets.Single().Id);
    }

    [Fact]
    public void AnEdgeWithAnAttachedEndOnAMissingInstanceIsDroppedAndAFloatingEndPasses()
    {
        var fragment = new Board();
        var present = AddShape(fragment, 0);
        fragment.AddEdge(
            new Edge(new PortEndpoint(present.Id, PortId.Right), new FloatingEndpoint(300, 0))
        );
        var broken = new Edge(
            new PortEndpoint(present.Id, PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        fragment.AddEdge(broken);

        var dropped = BoardFragment.DropEdgesWithMissingEnds(fragment);

        Assert.Equal([broken], dropped);
        Assert.Single(fragment.Edges);
    }

    [Fact]
    public void TheExtentOfAnEdgeOnlyFragmentSpansItsEnds()
    {
        var fragment = new Board();
        fragment.AddEdge(new Edge(new FloatingEndpoint(10, 80), new FloatingEndpoint(90, 20)));

        Assert.Equal(new Bounds(10, 20, 80, 60), BoardFragment.Extent(fragment));
    }

    [Fact]
    public void TheExtentUnionsInstancesWithFloatingEnds()
    {
        var fragment = new Board();
        var shape = AddShape(fragment, 0);
        fragment.AddEdge(
            new Edge(new PortEndpoint(shape.Id, PortId.Right), new FloatingEndpoint(300, -40))
        );

        Assert.Equal(new Bounds(0, -40, 300, 90), BoardFragment.Extent(fragment));
    }

    [Fact]
    public void TranslatingMovesInstancesAndFloatingEndsTogether()
    {
        var fragment = new Board();
        var shape = AddShape(fragment, 0);
        var attached = new PortEndpoint(shape.Id, PortId.Right);
        var edge = new Edge(attached, new FloatingEndpoint(300, 10));
        fragment.AddEdge(edge);

        BoardFragment.Translate(fragment, 15, -5);

        Assert.Equal(new Bounds(15, -5, 100, 50), shape.Bounds);
        Assert.Equal(attached, edge.Source);
        Assert.Equal(new FloatingEndpoint(315, 5), edge.Target);
    }

    [Fact]
    public void PlacingStacksTheFragmentAboveTheBoardKeepingItsOwnOrder()
    {
        var board = new Board();
        AddShape(board, 0, zIndex: 7);
        var fragment = new Board();
        var lower = AddShape(fragment, 0, zIndex: -3);
        var upper = AddShape(fragment, 0, zIndex: 40);

        var placement = BoardFragment.PlaceOnto(board, fragment);
        placement.Command.Apply();

        Assert.Equal(8, lower.ZIndex);
        Assert.Equal(9, upper.ZIndex);
        Assert.Equal(3, board.Components.Count);
    }

    [Fact]
    public void PlacingIsOneEntryThatUndoesWhole()
    {
        var board = new Board();
        var fragment = new Board();
        var first = AddShape(fragment, 0);
        var second = AddShape(fragment, 200);
        var group = new Group([first.Id, second.Id]);
        fragment.AddGroup(group);
        var edge = new Edge(new FloatingEndpoint(0, 0), new FloatingEndpoint(10, 10));
        fragment.AddEdge(edge);

        var placement = BoardFragment.PlaceOnto(board, fragment);
        placement.Command.Apply();

        Assert.Equal([group.Id], placement.TopLevelIds);
        Assert.Equal([edge.Id], placement.EdgeIds);
        Assert.NotNull(board.GetGroup(group.Id));

        placement.Command.Undo();

        Assert.Empty(board.Components);
        Assert.Empty(board.Groups);
        Assert.Empty(board.Edges);
    }

    [Fact]
    public void PlacingTheSameAssetTwiceStoresItOnce()
    {
        var board = new Board();
        var reference = board.AddAsset(AssetTestComponent.PngBytes, "image/png");
        var shown = new ComponentInstance(
            AssetTestComponent.Key,
            new AssetTestProps(reference),
            new Bounds(0, 0, 10, 10)
        );
        board.AddComponent(shown);

        BoardFragment
            .PlaceOnto(board, BoardFragment.WithFreshIds(Copy(board, [shown.Id])))
            .Command.Apply();

        Assert.Single(board.Assets);
        Assert.Equal(2, board.Components.Count);
    }

    [Fact]
    public void AnAssetWhoseIdIsNotTheHashOfItsBytesIsRefused()
    {
        var forged = new Board();
        forged.AddAsset(
            new Asset(Asset.IdFor([9, 9, 9]), "image/png", AssetTestComponent.PngBytes)
        );
        var board = new Board();

        var placement = BoardFragment.PlaceOnto(board, forged);

        Assert.Equal([Asset.IdFor([9, 9, 9])], placement.RejectedAssetIds);
        Assert.Empty(board.Assets);
    }

    [Fact]
    public void ACopyOfALockedInstanceAndEdgeIsLockedWithFreshIdsToo()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        shape.Locked = true;
        var other = AddShape(board, 200);
        var edge = Connect(board, shape, other);
        edge.Locked = true;

        var copy = BoardFragment.WithFreshIds(Copy(board, [shape.Id, other.Id], [edge.Id]));

        Assert.Single(copy.Components, instance => instance.Locked);
        Assert.True(copy.Edges.Single().Locked);
    }

    [Fact]
    public void WithoutLockedACutLeavesLockedInstancesAndEdgesBehind()
    {
        var board = new Board();
        var locked = AddShape(board, 0);
        locked.Locked = true;
        var first = AddShape(board, 200);
        var second = AddShape(board, 400);
        var lockedEdge = Connect(board, first, second);
        lockedEdge.Locked = true;

        var cutFragment = BoardFragment.Of(
            board,
            [locked.Id, first.Id, second.Id],
            [lockedEdge.Id],
            Registry,
            withoutLocked: true
        );

        Assert.Equal(
            new[] { first.Id, second.Id }.Order(),
            cutFragment.Components.Select(instance => instance.Id).Order()
        );
        Assert.Empty(cutFragment.Edges);
    }

    [Fact]
    public void WithoutLockedAGroupLeftWithOneMemberIsDissolvedAndOneLeftWithTwoIsKept()
    {
        var board = new Board();
        var locked = AddShape(board, 0);
        locked.Locked = true;
        var lone = AddShape(board, 200);
        var pair = new Group([locked.Id, lone.Id]);
        board.AddGroup(pair);
        var kept = new Group([AddShape(board, 400).Id, AddShape(board, 600).Id, locked.Id]);
        board.AddGroup(kept);

        var pairCutFragment = BoardFragment.Of(board, [pair.Id], [], Registry, withoutLocked: true);
        var keptCutFragment = BoardFragment.Of(board, [kept.Id], [], Registry, withoutLocked: true);

        Assert.Empty(pairCutFragment.Groups);
        Assert.Equal(lone.Id, pairCutFragment.Components.Single().Id);
        Assert.Equal(2, keptCutFragment.Groups.Single().MemberIds.Count);
        Assert.DoesNotContain(locked.Id, keptCutFragment.Groups.Single().MemberIds);
    }
}
