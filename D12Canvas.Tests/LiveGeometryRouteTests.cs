using D12Canvas.Model;
using D12Canvas.Pointer;
using Xunit;

namespace D12Canvas.Tests;

public class LiveGeometryRouteTests
{
    private readonly Board _board = new();
    private readonly GesturePreview _preview = new();

    private LiveGeometry Live => new(_board, _preview);

    private ComponentInstance AddShape(double x, double y)
    {
        var instance = new ComponentInstance("shape", new object(), new Bounds(x, y, 100, 100));
        _board.AddComponent(instance);
        return instance;
    }

    private RouteRequest RequestFor(IEdgeEndpoint source, IEdgeEndpoint target)
    {
        var edge = new Edge(source, target, routingStyle: EdgeRouting.Orthogonal);
        _board.AddEdge(edge);
        return Assert.IsType<RouteRequest>(Live.RouteRequestOf(edge));
    }

    [Fact]
    public void AStandardPortLeavesAlongItsOwnSideFromItsShape()
    {
        var shape = AddShape(0, 0);

        var request = RequestFor(
            new PortEndpoint(shape.Id, PortId.Top),
            new FloatingEndpoint(500, 500)
        );

        Assert.Equal(new RouteEnd(50, 0, PortId.Top, shape.Bounds), request.Source);
        Assert.Equal(EdgeRouting.Orthogonal, request.Style);
    }

    [Fact]
    public void ACustomPortLeavesAlongTheBorderItLiesOn()
    {
        var shape = AddShape(0, 0);
        var port = new PortDef(1, 0.25);
        shape.CustomPorts.Add(port);

        var request = RequestFor(
            new CustomPortEndpoint(shape.Id, port.Id),
            new FloatingEndpoint(-500, 0)
        );

        Assert.Equal(new RouteEnd(100, 25, PortId.Right, shape.Bounds), request.Source);
    }

    [Fact]
    public void AnAutoEndpointLeavesAlongTheSideItResolvesTo()
    {
        var shape = AddShape(0, 0);

        var request = RequestFor(new AutoPortEndpoint(shape.Id), new FloatingEndpoint(50, 400));

        Assert.Equal(new RouteEnd(50, 100, PortId.Bottom, shape.Bounds), request.Source);
    }

    [Fact]
    public void AFloatingEndHasNoShapeAndFacesTheOtherEnd()
    {
        var shape = AddShape(300, 0);

        var request = RequestFor(
            new FloatingEndpoint(0, 40),
            new PortEndpoint(shape.Id, PortId.Left)
        );

        Assert.Equal(new RouteEnd(0, 40, PortId.Right, null), request.Source);
    }

    [Fact]
    public void AShapeMovedByTheGestureRoutesFromItsPreviewedBounds()
    {
        var shape = AddShape(0, 0);
        var moved = new Bounds(200, 200, 100, 100);
        _preview.Publish(new Dictionary<Guid, Bounds> { [shape.Id] = moved });

        var request = RequestFor(
            new PortEndpoint(shape.Id, PortId.Right),
            new FloatingEndpoint(600, 250)
        );

        Assert.Equal(new RouteEnd(300, 250, PortId.Right, moved), request.Source);
    }

    [Fact]
    public void AnEndOnAMissingShapeHasNoRoute()
    {
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new FloatingEndpoint(0, 0)
        );

        Assert.Null(Live.RouteRequestOf(edge));
    }
}
