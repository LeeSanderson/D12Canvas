using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class RemoveEntityCommandTests
{
    [Fact]
    public void ApplyRemovesTheInstanceFromTheBoard()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        board.AddComponent(instance);
        var command = new RemoveEntityCommand(board, instance);

        command.Apply();

        Assert.Null(board.GetComponent(instance.Id));
    }

    [Fact]
    public void UndoRestoresTheSameInstanceIdentityBoundsAndProps()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps("hello"),
            new Bounds(10, 20, 30, 40)
        );
        board.AddComponent(instance);
        var command = new RemoveEntityCommand(board, instance);
        command.Apply();

        command.Undo();

        var restored = board.GetComponent(instance.Id);
        Assert.Same(instance, restored);
        Assert.Equal(instance.Id, restored!.Id);
        Assert.Equal(new Bounds(10, 20, 30, 40), restored.Bounds);
        Assert.Equal("hello", ((TestProps)restored.Props).Text);
    }

    [Fact]
    public void RedoRemovesTheInstanceAgain()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        board.AddComponent(instance);
        var command = new RemoveEntityCommand(board, instance);
        command.Apply();
        command.Undo();

        command.Apply(); // redo

        Assert.Null(board.GetComponent(instance.Id));
    }
}
