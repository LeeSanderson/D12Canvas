using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 48: the Edge/PortEndpoint model itself (ADR 0005) - Board's use of it (ResolveEndpoint,
// FindPortNear, Edges) is covered in BoardTests.
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
}
