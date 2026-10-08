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

    // While a pointer gesture owns the press, nothing writes the board except that gesture's own
    // release. Every library write goes through here, so the gate sits here: a Do, Undo or Redo
    // that arrives while locked does nothing, and the gesture carries on.
    public bool IsLocked { get; private set; }

    public void Lock() => IsLocked = true;

    public void Unlock() => IsLocked = false;

    // The most recently done/redone command, or null once it's been undone or the stack is
    // empty - lets a caller (arrow-key nudge's burst-coalescing) confirm nothing else has been
    // pushed or undone since a command it's holding a reference to, before mutating it in place.
    public ICommand? PeekUndo => _undoStack.Last?.Value;

    // Fires whenever Do/Undo/Redo/Retract actually mutates board content - not on a no-op Undo/Redo
    // against an empty stack. Carries no payload; a host owns its own dirty-tracking on top of it.
    public event EventHandler? Changed;

    private void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void Do(ICommand command)
    {
        if (IsLocked)
        {
            return;
        }

        command.Apply();

        _undoStack.AddLast(command);
        if (_undoStack.Count > _capacity)
        {
            _undoStack.RemoveFirst();
        }

        // A new gesture abandons whatever was undone - it can no longer be redone.
        _redoStack.Clear();
        NotifyChanged();
    }

    // Takes back a command still on top of the undo stack, by reference, as though it had never
    // been done: it is undone and dropped, and redo is left as it was. False when anything was
    // pushed or undone since, which leaves the caller to record its change as a new entry.
    public bool Retract(ICommand command)
    {
        if (IsLocked || !ReferenceEquals(PeekUndo, command))
        {
            return false;
        }

        _undoStack.RemoveLast();
        command.Undo();
        NotifyChanged();
        return true;
    }

    public void Undo()
    {
        if (IsLocked || _undoStack.Last is null)
        {
            return;
        }

        var command = _undoStack.Last.Value;
        _undoStack.RemoveLast();
        command.Undo();
        _redoStack.Push(command);
        NotifyChanged();
    }

    public void Redo()
    {
        if (IsLocked || !_redoStack.TryPop(out var command))
        {
            return;
        }

        command.Apply();
        _undoStack.AddLast(command);
        NotifyChanged();
    }
}
