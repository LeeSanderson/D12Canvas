using D12Canvas.Model;

namespace D12Canvas.History;

// Locking is a plain field write on an instance or an edge, the one command that acts on a locked
// entity, since unlocking is how a lock ends.
public sealed class ChangeLockedCommand : ICommand
{
    private readonly Action<bool> _write;
    private readonly bool _before;
    private readonly bool _after;

    public ChangeLockedCommand(ComponentInstance instance, bool after)
        : this(value => instance.Locked = value, instance.Locked, after) { }

    public ChangeLockedCommand(Edge edge, bool after)
        : this(value => edge.Locked = value, edge.Locked, after) { }

    private ChangeLockedCommand(Action<bool> write, bool before, bool after)
    {
        _write = write;
        _before = before;
        _after = after;
    }

    public void Apply() => _write(_after);

    public void Undo() => _write(_before);
}
