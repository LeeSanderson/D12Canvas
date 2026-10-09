using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class RemoveCustomPortCommandTests
{
    private static ComponentInstance InstanceWithPorts(params PortDef[] ports) =>
        new("sticky-note", new TestProps(), new Bounds(0, 0, 100, 100), customPorts: ports);

    [Fact]
    public void ApplyRemovesThePortFromTheInstance()
    {
        var kept = new PortDef(0, 0.5);
        var removed = new PortDef(0.25, 0);
        var instance = InstanceWithPorts(kept, removed);

        new RemoveCustomPortCommand(instance, removed).Apply();

        Assert.Equal(new[] { kept }, instance.CustomPorts);
    }

    [Fact]
    public void UndoRestoresThePortAtItsOriginalIndex()
    {
        var first = new PortDef(0, 0.5);
        var middle = new PortDef(0.25, 0);
        var last = new PortDef(1, 0.75);
        var instance = InstanceWithPorts(first, middle, last);
        var command = new RemoveCustomPortCommand(instance, middle);
        command.Apply();

        command.Undo();

        Assert.Equal(new[] { first, middle, last }, instance.CustomPorts);
    }

    [Fact]
    public void RedoRemovesTheSamePortAgain()
    {
        var first = new PortDef(0, 0.5);
        var removed = new PortDef(0.25, 0);
        var instance = InstanceWithPorts(first, removed);
        var command = new RemoveCustomPortCommand(instance, removed);
        command.Apply();
        command.Undo();

        command.Apply();

        Assert.Equal(new[] { first }, instance.CustomPorts);
    }
}
