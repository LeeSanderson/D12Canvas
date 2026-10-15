using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ChangeLockedCommandTests
{
    private static ComponentInstance Instance(bool locked = false) =>
        new("sticky-note", new TestProps(), new Bounds(0, 0, 50, 50), locked: locked);

    [Fact]
    public void ApplyLocksAnInstanceAndUndoUnlocksIt()
    {
        var instance = Instance();
        var command = new ChangeLockedCommand(instance, after: true);

        command.Apply();
        Assert.True(instance.Locked);

        command.Undo();
        Assert.False(instance.Locked);
    }

    [Fact]
    public void ApplyUnlocksALockedInstanceAndUndoLocksItAgain()
    {
        var instance = Instance(locked: true);
        var command = new ChangeLockedCommand(instance, after: false);

        command.Apply();
        Assert.False(instance.Locked);

        command.Undo();
        Assert.True(instance.Locked);
    }

    [Fact]
    public void ApplyLocksAnEdgeAndUndoUnlocksIt()
    {
        var edge = new Edge(new FloatingEndpoint(0, 0), new FloatingEndpoint(10, 0));
        var command = new ChangeLockedCommand(edge, after: true);

        command.Apply();
        Assert.True(edge.Locked);

        command.Undo();
        Assert.False(edge.Locked);
    }
}
