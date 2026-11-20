namespace D12Canvas.History;

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

    public bool IsLocked { get; private set; }

    public void Lock() => IsLocked = true;

    public void Unlock() => IsLocked = false;

    public ICommand? PeekUndo => _undoStack.Last?.Value;

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

        _redoStack.Clear();
        NotifyChanged();
    }

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
