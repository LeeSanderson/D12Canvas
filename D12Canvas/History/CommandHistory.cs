namespace D12Canvas.History;

// A session-scoped, in-memory, capped stack of Commands backing undo/redo for the current Board -
// never part of the persisted envelope, never tracked across a reload. Do() both applies a
// command and records it, so a caller can't record a mutation it forgot to apply.
public sealed class CommandHistory
{
    public const int DefaultCapacity = 1000;

    private readonly int _capacity;
    private readonly LinkedList<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();

    public CommandHistory(int capacity = DefaultCapacity)
    {
        _capacity = capacity;
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    // The most recently done/redone command, or null once it's been undone or the stack is
    // empty - lets a caller (arrow-key nudge's burst-coalescing) confirm nothing else has been
    // pushed or undone since a command it's holding a reference to, before mutating it in place.
    public ICommand? PeekUndo => _undoStack.Last?.Value;

    public void Do(ICommand command)
    {
        command.Apply();

        _undoStack.AddLast(command);
        if (_undoStack.Count > _capacity)
        {
            _undoStack.RemoveFirst();
        }

        // A new gesture abandons whatever was undone - it can no longer be redone.
        _redoStack.Clear();
    }

    public void Undo()
    {
        if (_undoStack.Last is null)
        {
            return;
        }

        var command = _undoStack.Last.Value;
        _undoStack.RemoveLast();
        command.Undo();
        _redoStack.Push(command);
    }

    public void Redo()
    {
        if (!_redoStack.TryPop(out var command))
        {
            return;
        }

        command.Apply();
        _undoStack.AddLast(command);
    }
}
