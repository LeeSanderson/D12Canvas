using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class AddEdgeCommandTests
{
    [Fact]
    public void ApplyAddsTheEdgeToTheBoard()
    {
        var board = new Board();
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        var command = new AddEdgeCommand(board, edge);

        command.Apply();

        Assert.Same(edge, board.GetEdge(edge.Id));
    }

    [Fact]
    public void UndoRemovesTheEdgeFromTheBoard()
    {
        var board = new Board();
        var edge = new Edge(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );
        var command = new AddEdgeCommand(board, edge);
        command.Apply();

        command.Undo();

        Assert.Null(board.GetEdge(edge.Id));
    }

    [Fact]
    public void RedoRestoresTheSameEdgeIdentityAndAttachments()
    {
        var source = new PortEndpoint(Guid.NewGuid(), PortId.Right);
        var target = new FloatingEndpoint(10, 20);
        var board = new Board();
        var edge = new Edge(source, target);
        var command = new AddEdgeCommand(board, edge);
        command.Apply();
        command.Undo();

        command.Apply(); // redo

        var restored = board.GetEdge(edge.Id);
        Assert.Same(edge, restored);
        Assert.Equal(source, restored!.Source);
        Assert.Equal(target, restored.Target);
    }
}
