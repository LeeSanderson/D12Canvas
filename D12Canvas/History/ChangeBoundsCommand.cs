using D12Canvas.Model;

namespace D12Canvas.History;

// ADR 0007: covers both move and resize as one command, since Bounds is already a single
// combined position+size struct - which of the two a gesture was is never tracked here.
public sealed class ChangeBoundsCommand : ICommand
{
    private readonly ComponentInstance _instance;
    private readonly Bounds _before;
    private readonly Bounds _after;

    public ChangeBoundsCommand(ComponentInstance instance, Bounds before, Bounds after)
    {
        _instance = instance;
        _before = before;
        _after = after;
    }

    public void Apply() => _instance.Bounds = _after;

    public void Undo() => _instance.Bounds = _before;
}
