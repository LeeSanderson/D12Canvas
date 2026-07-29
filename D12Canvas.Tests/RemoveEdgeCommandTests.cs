using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class RemoveEdgeCommandTests
{
    [Fact]
    public void ApplyRemovesTheEdgeFromTheBoard()
    {
        var board = new Board();
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        board.AddEdge(edge);
        var command = new RemoveEdgeCommand(board, edge);

        command.Apply();

        Assert.Null(board.GetEdge(edge.Id));
    }

    [Fact]
    public void UndoRestoresTheSameEdgeIdentityAndAttachmentsWhenBothEndpointsAreAttached()
    {
        var source = new PortEndpoint(Guid.NewGuid(), PortId.Right);
        var target = new PortEndpoint(Guid.NewGuid(), PortId.Left);
        var board = new Board();
        var edge = new Edge(source, target);
        board.AddEdge(edge);
        var command = new RemoveEdgeCommand(board, edge);
        command.Apply();

        command.Undo();

        var restored = board.GetEdge(edge.Id);
        Assert.Same(edge, restored);
        Assert.Equal(source, restored!.Source);
        Assert.Equal(target, restored.Target);
    }

    [Fact]
    public void UndoRestoresAFloatingEndpointIntact()
    {
        var source = new PortEndpoint(Guid.NewGuid(), PortId.Right);
        var target = new FloatingEndpoint(190, 400);
        var board = new Board();
        var edge = new Edge(source, target);
        board.AddEdge(edge);
        var command = new RemoveEdgeCommand(board, edge);
        command.Apply();

        command.Undo();

        var restored = board.GetEdge(edge.Id);
        Assert.Same(edge, restored);
        Assert.Equal(source, restored!.Source);
        Assert.Equal(target, restored.Target);
    }

    [Fact]
    public void RedoRemovesTheEdgeAgain()
    {
        var board = new Board();
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        board.AddEdge(edge);
        var command = new RemoveEdgeCommand(board, edge);
        command.Apply();
        command.Undo();

        command.Apply(); // redo

        Assert.Null(board.GetEdge(edge.Id));
    }
}
