using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

// The content extent is every instance's committed bounds unioned with every edge end that
// resolves. Restricted to some instances and edges, the same computation is what framing a
// selection measures.
public class BoardContentExtentTests
{
    private static ComponentInstance AddInstance(Board board, Bounds bounds)
    {
        var instance = new ComponentInstance("test", new TestProps(), bounds);
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void AnEmptyBoardHasNoExtent()
    {
        Assert.Null(new Board().ContentExtent());
    }

    [Fact]
    public void TheExtentUnionsEveryInstancesBounds()
    {
        var board = new Board();
        AddInstance(board, new Bounds(100, 200, 50, 50));
        AddInstance(board, new Bounds(-300, 400, 100, 80));

        Assert.Equal(new Bounds(-300, 200, 450, 280), board.ContentExtent());
    }

    [Fact]
    public void ABoardHoldingOnlyAFloatingEdgeHasThatEdgeAsItsExtent()
    {
        var board = new Board();
        board.AddEdge(new Edge(new FloatingEndpoint(5000, 40), new FloatingEndpoint(5300, -60)));

        Assert.Equal(new Bounds(5000, -60, 300, 100), board.ContentExtent());
    }

    [Fact]
    public void AFloatingEndBeyondEveryInstanceWidensTheExtent()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(0, 0, 100, 100));
        board.AddEdge(
            new Edge(new PortEndpoint(shape.Id, PortId.Right), new FloatingEndpoint(900, 50))
        );

        Assert.Equal(new Bounds(0, 0, 900, 100), board.ContentExtent());
    }

    [Fact]
    public void AnEndThatNoLongerResolvesIsSkippedRatherThanReadAsTheOrigin()
    {
        var board = new Board();
        AddInstance(board, new Bounds(1000, 1000, 100, 100));
        board.AddEdge(
            new Edge(
                new PortEndpoint(Guid.NewGuid(), PortId.Left),
                new FloatingEndpoint(1200, 1050)
            )
        );

        Assert.Equal(new Bounds(1000, 1000, 200, 100), board.ContentExtent());
    }

    [Fact]
    public void TheExtentOfSomeEntitiesCountsOnlyThose()
    {
        var board = new Board();
        var kept = AddInstance(board, new Bounds(0, 0, 100, 100));
        AddInstance(board, new Bounds(5000, 5000, 100, 100));
        var edge = new Edge(new FloatingEndpoint(-200, 30), new FloatingEndpoint(-100, 30));
        board.AddEdge(edge);
        board.AddEdge(new Edge(new FloatingEndpoint(9000, 0), new FloatingEndpoint(9100, 0)));

        Assert.Equal(new Bounds(-200, 0, 300, 100), board.ExtentOf([kept], [edge]));
        Assert.Null(board.ExtentOf([], []));
    }
}
