using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ChangeEdgeEndpointCommandTests
{
    private static readonly PortEndpoint SourcePort = new(Guid.NewGuid(), PortId.Right);
    private static readonly PortEndpoint TargetPort = new(Guid.NewGuid(), PortId.Left);

    [Fact]
    public void ApplyWritesTheAfterEndpointOntoTheSource()
    {
        var edge = new Edge(SourcePort, TargetPort);
        var command = new ChangeEdgeEndpointCommand(
            edge,
            isSource: true,
            before: SourcePort,
            after: new FloatingEndpoint(10, 20)
        );

        command.Apply();

        Assert.Equal(new FloatingEndpoint(10, 20), edge.Source);
        Assert.Equal(TargetPort, edge.Target);
    }

    [Fact]
    public void ApplyWritesTheAfterEndpointOntoTheTarget()
    {
        var edge = new Edge(SourcePort, TargetPort);
        var command = new ChangeEdgeEndpointCommand(
            edge,
            isSource: false,
            before: TargetPort,
            after: new FloatingEndpoint(10, 20)
        );

        command.Apply();

        Assert.Equal(SourcePort, edge.Source);
        Assert.Equal(new FloatingEndpoint(10, 20), edge.Target);
    }

    [Fact]
    public void UndoPutsTheBeforeEndpointBack()
    {
        var edge = new Edge(SourcePort, TargetPort);
        var command = new ChangeEdgeEndpointCommand(
            edge,
            isSource: false,
            before: TargetPort,
            after: new FloatingEndpoint(10, 20)
        );
        command.Apply();

        command.Undo();

        Assert.Equal(TargetPort, edge.Target);
    }
}
