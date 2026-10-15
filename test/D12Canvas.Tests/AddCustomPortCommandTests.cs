using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class AddCustomPortCommandTests
{
    private static ComponentInstance NewInstance() =>
        new("sticky-note", new TestProps(), new Bounds(0, 0, 100, 100));

    [Fact]
    public void ApplyAddsThePortToTheInstance()
    {
        var instance = NewInstance();
        var port = new PortDef(0.25, 0);
        var command = new AddCustomPortCommand(instance, port);

        command.Apply();

        Assert.Equal(new[] { port }, instance.CustomPorts);
    }

    [Fact]
    public void UndoRemovesExactlyThePortThatWasAdded()
    {
        var instance = NewInstance();
        var earlierPort = new PortDef(0, 0.5);
        instance.CustomPorts.Add(earlierPort);
        var port = new PortDef(0.25, 0);
        var command = new AddCustomPortCommand(instance, port);
        command.Apply();

        command.Undo();

        Assert.Equal(new[] { earlierPort }, instance.CustomPorts);
    }

    [Fact]
    public void RedoReappliesTheSamePort()
    {
        var instance = NewInstance();
        var port = new PortDef(0.25, 0);
        var command = new AddCustomPortCommand(instance, port);
        command.Apply();
        command.Undo();

        command.Apply(); // redo

        Assert.Equal(new[] { port }, instance.CustomPorts);
    }

    [Fact]
    public void AddingAPortToOneInstanceNeverAffectsAnother()
    {
        var instance = NewInstance();
        var other = NewInstance();
        var command = new AddCustomPortCommand(instance, new PortDef(0.25, 0));

        command.Apply();

        Assert.Empty(other.CustomPorts);
    }
}
