using System.Collections.Generic;
using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class CompositeCommandTests
{
    [Fact]
    public void ApplyRunsEveryChildCommandInOrder()
    {
        var order = new List<int>();
        var command = new CompositeCommand(
            new ICommand[] { new RecordingCommand(order, 1), new RecordingCommand(order, 2) }
        );

        command.Apply();

        Assert.Equal(new[] { 1, 2 }, order);
    }

    [Fact]
    public void UndoRunsEveryChildCommandInReverseOrder()
    {
        var order = new List<int>();
        var command = new CompositeCommand(
            new ICommand[] { new RecordingCommand(order, 1), new RecordingCommand(order, 2) }
        );

        command.Undo();

        Assert.Equal(new[] { 2, 1 }, order);
    }

    [Fact]
    public void ApplyAndUndoMutateEveryChildsOwnEntity()
    {
        var first = new ComponentInstance("sticky-note", new TestProps(), new Bounds(0, 0, 50, 50));
        var second = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(100, 0, 50, 50)
        );
        var command = new CompositeCommand(
            new ICommand[]
            {
                new ChangeBoundsCommand(
                    first,
                    new Bounds(0, 0, 50, 50),
                    new Bounds(10, 10, 50, 50)
                ),
                new ChangeBoundsCommand(
                    second,
                    new Bounds(100, 0, 50, 50),
                    new Bounds(110, 10, 50, 50)
                ),
            }
        );

        command.Apply();

        Assert.Equal(new Bounds(10, 10, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(110, 10, 50, 50), second.Bounds);

        command.Undo();

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(100, 0, 50, 50), second.Bounds);
    }

    private sealed class RecordingCommand : ICommand
    {
        private readonly List<int> _order;
        private readonly int _id;

        public RecordingCommand(List<int> order, int id)
        {
            _order = order;
            _id = id;
        }

        public void Apply() => _order.Add(_id);

        public void Undo() => _order.Add(_id);
    }
}
