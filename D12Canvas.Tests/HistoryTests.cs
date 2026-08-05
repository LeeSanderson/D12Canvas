using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class HistoryTests
{
    [Fact]
    public void DoAppliesTheCommandImmediately()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        var history = new CommandHistory();

        history.Do(
            new ChangeBoundsCommand(instance, new Bounds(0, 0, 50, 50), new Bounds(10, 10, 50, 50))
        );

        Assert.Equal(new Bounds(10, 10, 50, 50), instance.Bounds);
    }

    [Fact]
    public void UndoRevertsTheMostRecentlyDoneCommand()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        var history = new CommandHistory();
        history.Do(
            new ChangeBoundsCommand(instance, new Bounds(0, 0, 50, 50), new Bounds(10, 10, 50, 50))
        );

        history.Undo();

        Assert.Equal(new Bounds(0, 0, 50, 50), instance.Bounds);
    }

    [Fact]
    public void RedoReappliesTheMostRecentlyUndoneCommand()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        var history = new CommandHistory();
        history.Do(
            new ChangeBoundsCommand(instance, new Bounds(0, 0, 50, 50), new Bounds(10, 10, 50, 50))
        );
        history.Undo();

        history.Redo();

        Assert.Equal(new Bounds(10, 10, 50, 50), instance.Bounds);
    }

    [Fact]
    public void UndoOnAnEmptyHistoryIsANoOp()
    {
        var history = new CommandHistory();

        history.Undo();

        Assert.False(history.CanUndo);
    }

    [Fact]
    public void RedoWithNothingUndoneIsANoOp()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        var history = new CommandHistory();
        history.Do(
            new ChangeBoundsCommand(instance, new Bounds(0, 0, 50, 50), new Bounds(10, 10, 50, 50))
        );

        history.Redo();

        Assert.Equal(new Bounds(10, 10, 50, 50), instance.Bounds);
    }

    [Fact]
    public void ANewGestureAfterUndoClearsTheRedoStack()
    {
        var first = new ComponentInstance("sticky-note", new TestProps(), new Bounds(0, 0, 50, 50));
        var second = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(100, 0, 50, 50)
        );
        var history = new CommandHistory();
        history.Do(
            new ChangeBoundsCommand(first, new Bounds(0, 0, 50, 50), new Bounds(10, 10, 50, 50))
        );
        history.Undo();

        history.Do(
            new ChangeBoundsCommand(second, new Bounds(100, 0, 50, 50), new Bounds(120, 10, 50, 50))
        );

        Assert.False(history.CanRedo);
        history.Redo(); // no-op: the new gesture already abandoned the undone command

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds); // never reapplied
        Assert.Equal(new Bounds(120, 10, 50, 50), second.Bounds); // unaffected by the no-op redo
    }

    [Fact]
    public void UndoPastTheCapacityStopsAtTheOldestRetainedEntry()
    {
        var history = new CommandHistory(capacity: 2);
        var oldest = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var middle = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );
        var newest = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 10, 10)
        );

        // Three gestures recorded against a capacity of 2 - the oldest entry (touching `oldest`)
        // is dropped from the buffer before any undo happens.
        history.Do(
            new ChangeBoundsCommand(oldest, new Bounds(0, 0, 10, 10), new Bounds(1, 1, 10, 10))
        );
        history.Do(
            new ChangeBoundsCommand(middle, new Bounds(0, 0, 10, 10), new Bounds(2, 2, 10, 10))
        );
        history.Do(
            new ChangeBoundsCommand(newest, new Bounds(0, 0, 10, 10), new Bounds(3, 3, 10, 10))
        );

        history.Undo();
        history.Undo();
        history.Undo(); // nothing left to undo - the third gesture was already dropped

        Assert.Equal(new Bounds(1, 1, 10, 10), oldest.Bounds); // dropped before it could be undone
        Assert.Equal(new Bounds(0, 0, 10, 10), middle.Bounds);
        Assert.Equal(new Bounds(0, 0, 10, 10), newest.Bounds);
    }

    [Fact]
    public void ChangedFiresOnDoUndoAndRedo()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        var history = new CommandHistory();
        var changedCount = 0;
        history.Changed += (_, _) => changedCount++;

        history.Do(
            new ChangeBoundsCommand(instance, new Bounds(0, 0, 50, 50), new Bounds(10, 10, 50, 50))
        );
        Assert.Equal(1, changedCount);

        history.Undo();
        Assert.Equal(2, changedCount);

        history.Redo();
        Assert.Equal(3, changedCount);
    }

    [Fact]
    public void ChangedDoesNotFireOnANoOpUndoOrRedo()
    {
        var history = new CommandHistory();
        var changedCount = 0;
        history.Changed += (_, _) => changedCount++;

        history.Undo();
        history.Redo();

        Assert.Equal(0, changedCount);
    }

    [Fact]
    public void CanUndoAndCanRedoReflectStackState()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        var history = new CommandHistory();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);

        history.Do(
            new ChangeBoundsCommand(instance, new Bounds(0, 0, 50, 50), new Bounds(10, 10, 50, 50))
        );

        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        history.Undo();

        Assert.False(history.CanUndo);
        Assert.True(history.CanRedo);
    }
}
