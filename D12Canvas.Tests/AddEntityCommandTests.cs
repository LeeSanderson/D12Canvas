using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class AddEntityCommandTests
{
    [Fact]
    public void ApplyAddsTheInstanceToTheBoard()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        var command = new AddEntityCommand(board, instance);

        command.Apply();

        Assert.Same(instance, board.GetComponent(instance.Id));
    }

    [Fact]
    public void UndoRemovesTheInstanceFromTheBoard()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        var command = new AddEntityCommand(board, instance);
        command.Apply();

        command.Undo();

        Assert.Null(board.GetComponent(instance.Id));
    }

    [Fact]
    public void RedoRestoresTheSameInstanceIdentityBoundsAndProps()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps("hello"),
            new Bounds(10, 20, 30, 40)
        );
        var command = new AddEntityCommand(board, instance);
        command.Apply();
        command.Undo();

        command.Apply(); // redo

        var restored = board.GetComponent(instance.Id);
        Assert.Same(instance, restored);
        Assert.Equal(instance.Id, restored!.Id);
        Assert.Equal(new Bounds(10, 20, 30, 40), restored.Bounds);
        Assert.Equal("hello", ((TestProps)restored.Props).Text);
    }
}
