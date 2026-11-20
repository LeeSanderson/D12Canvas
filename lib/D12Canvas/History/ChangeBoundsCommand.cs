using D12Canvas.Model;

namespace D12Canvas.History;

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
