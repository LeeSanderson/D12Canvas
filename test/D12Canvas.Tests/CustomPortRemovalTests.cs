using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class CustomPortRemovalTests
{
    private readonly Board _board = new();
    private readonly PortDef _first = new(0, 0.5);
    private readonly PortDef _removed = new(0.25, 0);
    private readonly PortDef _last = new(1, 0.75);
    private readonly ComponentInstance _shape;
    private readonly ComponentInstance _other;

    public CustomPortRemovalTests()
    {
        _shape = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100),
            customPorts: [_first, _removed, _last]
        );
        _other = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(300, 0, 100, 100)
        );
        _board.AddComponent(_shape);
        _board.AddComponent(_other);
    }

    private CustomPortEndpoint RemovedEnd => new(_shape.Id, _removed.Id);

    private Edge AddEdge(IEdgeEndpoint source, IEdgeEndpoint target, bool locked = false)
    {
        var edge = new Edge(source, target, locked: locked);
        _board.AddEdge(edge);
        return edge;
    }

    [Fact]
    public void RemovingAPortWithTwoPinnedEdgesMakesBothEndsAutoAndLeavesTheOtherEnds()
    {
        var outgoing = AddEdge(RemovedEnd, new PortEndpoint(_other.Id, PortId.Left));
        var incoming = AddEdge(new FloatingEndpoint(500, 500), RemovedEnd);

        var command = CustomPortRemoval.Compose(_board, _shape, _removed.Id);
        Assert.NotNull(command);
        command.Apply();

        Assert.Equal(new[] { _first, _last }, _shape.CustomPorts);
        Assert.Equal(new AutoPortEndpoint(_shape.Id), outgoing.Source);
        Assert.Equal(new PortEndpoint(_other.Id, PortId.Left), outgoing.Target);
        Assert.Equal(new FloatingEndpoint(500, 500), incoming.Source);
        Assert.Equal(new AutoPortEndpoint(_shape.Id), incoming.Target);
    }

    [Fact]
    public void OneUndoRestoresThePortAtItsIndexAndRepinsEveryEnd()
    {
        var outgoing = AddEdge(RemovedEnd, new PortEndpoint(_other.Id, PortId.Left));
        var incoming = AddEdge(new FloatingEndpoint(500, 500), RemovedEnd);
        var command = CustomPortRemoval.Compose(_board, _shape, _removed.Id)!;
        command.Apply();

        command.Undo();

        Assert.Equal(new[] { _first, _removed, _last }, _shape.CustomPorts);
        Assert.Equal(RemovedEnd, outgoing.Source);
        Assert.Equal(RemovedEnd, incoming.Target);
    }

    [Fact]
    public void AnEdgePinnedToAnotherPortIsNotTouched()
    {
        var elsewhere = new CustomPortEndpoint(_shape.Id, _first.Id);
        var edge = AddEdge(elsewhere, new PortEndpoint(_other.Id, PortId.Left));

        CustomPortRemoval.Compose(_board, _shape, _removed.Id)!.Apply();

        Assert.Equal(elsewhere, edge.Source);
    }

    [Fact]
    public void AnEdgeWhoseOtherEndIsAutoOnTheSameShapeStillResolves()
    {
        var edge = AddEdge(RemovedEnd, new AutoPortEndpoint(_shape.Id));

        CustomPortRemoval.Compose(_board, _shape, _removed.Id)!.Apply();

        Assert.NotNull(_board.ResolveEnd(edge, isSource: true));
        Assert.NotNull(_board.ResolveEnd(edge, isSource: false));
    }

    [Fact]
    public void ALockedShapeRefusesRemoval()
    {
        _shape.Locked = true;

        Assert.Null(CustomPortRemoval.Compose(_board, _shape, _removed.Id));
    }

    [Fact]
    public void ALockedPinnedEdgeRefusesRemoval()
    {
        AddEdge(RemovedEnd, new PortEndpoint(_other.Id, PortId.Left), locked: true);

        Assert.Null(CustomPortRemoval.Compose(_board, _shape, _removed.Id));
    }

    [Fact]
    public void ALockedEdgeOnAnotherPortDoesNotRefuseRemoval()
    {
        AddEdge(
            new CustomPortEndpoint(_shape.Id, _first.Id),
            new PortEndpoint(_other.Id, PortId.Left),
            locked: true
        );

        Assert.NotNull(CustomPortRemoval.Compose(_board, _shape, _removed.Id));
    }

    [Fact]
    public void AnUnknownPortRefusesRemoval()
    {
        Assert.Null(CustomPortRemoval.Compose(_board, _shape, Guid.NewGuid()));
    }
}
