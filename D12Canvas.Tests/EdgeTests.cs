using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

// The Edge/PortEndpoint model itself - Board's use of it (ResolveEndpoint, FindPortNear, Edges)
// is covered in BoardTests.
public class EdgeTests
{
    [Fact]
    public void EdgeGeneratesAnIdWhenNoneIsProvided()
    {
        var source = new PortEndpoint(Guid.NewGuid(), PortId.Right);
        var target = new PortEndpoint(Guid.NewGuid(), PortId.Left);

        var edge = new Edge(source, target);

        Assert.NotEqual(Guid.Empty, edge.Id);
    }

    [Fact]
    public void EdgeKeepsAnExplicitlyProvidedId()
    {
        var id = Guid.NewGuid();
        var source = new PortEndpoint(Guid.NewGuid(), PortId.Right);
        var target = new PortEndpoint(Guid.NewGuid(), PortId.Left);

        var edge = new Edge(source, target, id);

        Assert.Equal(id, edge.Id);
    }

    [Fact]
    public void EdgeExposesItsSourceAndTarget()
    {
        var source = new PortEndpoint(Guid.NewGuid(), PortId.Top);
        var target = new PortEndpoint(Guid.NewGuid(), PortId.Bottom);

        var edge = new Edge(source, target);

        Assert.Equal(source, edge.Source);
        Assert.Equal(target, edge.Target);
    }

    [Fact]
    public void PortEndpointsWithTheSameComponentAndPortAreEqual()
    {
        var componentId = Guid.NewGuid();

        Assert.Equal(
            new PortEndpoint(componentId, PortId.Top),
            new PortEndpoint(componentId, PortId.Top)
        );
    }

    [Fact]
    public void PortEndpointsWithDifferentPortsAreNotEqual()
    {
        var componentId = Guid.NewGuid();

        Assert.NotEqual(
            new PortEndpoint(componentId, PortId.Top),
            new PortEndpoint(componentId, PortId.Bottom)
        );
    }

    [Theory]
    [InlineData(PortId.Top, 0.5, 0)]
    [InlineData(PortId.Right, 1, 0.5)]
    [InlineData(PortId.Bottom, 0.5, 1)]
    [InlineData(PortId.Left, 0, 0.5)]
    public void StandardPortsFractionOfMatchesEachBorderCenter(
        PortId portId,
        double expectedFractionX,
        double expectedFractionY
    )
    {
        var (fractionX, fractionY) = StandardPorts.FractionOf(portId);

        Assert.Equal(expectedFractionX, fractionX);
        Assert.Equal(expectedFractionY, fractionY);
    }

    [Fact]
    public void StandardPortsAllListsExactlyTheFourBorderCenters()
    {
        Assert.Equal(
            new[] { PortId.Top, PortId.Right, PortId.Bottom, PortId.Left },
            StandardPorts.All
        );
    }

    // An edge not given explicit style defaults to the front-loaded single-directed-arrow case -
    // straight routing, no arrow at the source, an arrow at the target.
    [Fact]
    public void EdgeDefaultsToStraightRoutingWithAnArrowOnlyAtTheTarget()
    {
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );

        Assert.Equal(EdgeRouting.Straight, edge.RoutingStyle);
        Assert.Equal(ArrowStyle.None, edge.SourceArrow);
        Assert.Equal(ArrowStyle.Arrow, edge.TargetArrow);
    }

    [Fact]
    public void EdgeKeepsExplicitlyProvidedRoutingAndArrowStyles()
    {
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left),
            routingStyle: EdgeRouting.Curved,
            sourceArrow: ArrowStyle.Arrow,
            targetArrow: ArrowStyle.None
        );

        Assert.Equal(EdgeRouting.Curved, edge.RoutingStyle);
        Assert.Equal(ArrowStyle.Arrow, edge.SourceArrow);
        Assert.Equal(ArrowStyle.None, edge.TargetArrow);
    }

    [Fact]
    public void EdgesRoutingAndArrowStylesAreIndependentlyMutable()
    {
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        var other = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );

        edge.RoutingStyle = EdgeRouting.Orthogonal;
        edge.SourceArrow = ArrowStyle.Arrow;
        edge.TargetArrow = ArrowStyle.None;

        Assert.Equal(EdgeRouting.Orthogonal, edge.RoutingStyle);
        Assert.Equal(ArrowStyle.Arrow, edge.SourceArrow);
        Assert.Equal(ArrowStyle.None, edge.TargetArrow);
        Assert.Equal(EdgeRouting.Straight, other.RoutingStyle);
        Assert.Equal(ArrowStyle.None, other.SourceArrow);
        Assert.Equal(ArrowStyle.Arrow, other.TargetArrow);
    }

    // A label is a full embedded ComponentInstance, not a separate entity - absent by default,
    // since not every edge has one.
    [Fact]
    public void EdgeDefaultsToNoLabel()
    {
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );

        Assert.Null(edge.Label);
    }

    [Fact]
    public void EdgeKeepsAnExplicitlyProvidedLabel()
    {
        var label = new ComponentInstance("text", new object(), new Bounds(0, 0, 80, 24));

        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left),
            label: label
        );

        Assert.Same(label, edge.Label);
    }

    [Fact]
    public void EdgesLabelsAreIndependentlyMutable()
    {
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        var other = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        var label = new ComponentInstance("text", new object(), new Bounds(0, 0, 80, 24));

        edge.Label = label;

        Assert.Same(label, edge.Label);
        Assert.Null(other.Label);
    }
}
