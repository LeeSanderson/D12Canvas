using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ChangeEdgeStyleCommandTests
{
    private static Edge NewEdge() =>
        new(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );

    [Fact]
    public void ApplySetsTheEdgesRoutingAndArrowStylesToTheAfterSnapshot()
    {
        var edge = NewEdge();
        var command = new ChangeEdgeStyleCommand(
            edge,
            before: new EdgeStyle(EdgeRouting.Straight, ArrowStyle.None, ArrowStyle.Arrow),
            after: new EdgeStyle(EdgeRouting.Orthogonal, ArrowStyle.Arrow, ArrowStyle.None)
        );

        command.Apply();

        Assert.Equal(EdgeRouting.Orthogonal, edge.RoutingStyle);
        Assert.Equal(ArrowStyle.Arrow, edge.SourceArrow);
        Assert.Equal(ArrowStyle.None, edge.TargetArrow);
    }

    [Fact]
    public void UndoRestoresTheEdgesRoutingAndArrowStylesToTheBeforeSnapshot()
    {
        var edge = NewEdge();
        var command = new ChangeEdgeStyleCommand(
            edge,
            before: new EdgeStyle(EdgeRouting.Straight, ArrowStyle.None, ArrowStyle.Arrow),
            after: new EdgeStyle(EdgeRouting.Curved, ArrowStyle.Arrow, ArrowStyle.Arrow)
        );
        command.Apply();

        command.Undo();

        Assert.Equal(EdgeRouting.Straight, edge.RoutingStyle);
        Assert.Equal(ArrowStyle.None, edge.SourceArrow);
        Assert.Equal(ArrowStyle.Arrow, edge.TargetArrow);
    }

    [Fact]
    public void ChangingOneEdgesStyleNeverAffectsAnotherEdge()
    {
        var edge = NewEdge();
        var other = NewEdge();
        var command = new ChangeEdgeStyleCommand(
            edge,
            before: new EdgeStyle(EdgeRouting.Straight, ArrowStyle.None, ArrowStyle.Arrow),
            after: new EdgeStyle(EdgeRouting.Orthogonal, ArrowStyle.Arrow, ArrowStyle.Arrow)
        );

        command.Apply();

        Assert.Equal(EdgeRouting.Straight, other.RoutingStyle);
        Assert.Equal(ArrowStyle.None, other.SourceArrow);
        Assert.Equal(ArrowStyle.Arrow, other.TargetArrow);
    }

    [Fact]
    public void RedoReappliesTheAfterSnapshot()
    {
        var edge = NewEdge();
        var command = new ChangeEdgeStyleCommand(
            edge,
            before: new EdgeStyle(EdgeRouting.Straight, ArrowStyle.None, ArrowStyle.Arrow),
            after: new EdgeStyle(EdgeRouting.Curved, ArrowStyle.Arrow, ArrowStyle.None)
        );
        command.Apply();
        command.Undo();

        command.Apply(); // redo

        Assert.Equal(EdgeRouting.Curved, edge.RoutingStyle);
        Assert.Equal(ArrowStyle.Arrow, edge.SourceArrow);
        Assert.Equal(ArrowStyle.None, edge.TargetArrow);
    }
}
