using D12Canvas.Model;

namespace D12Canvas.History;

// ADR 0008: layering is a plain field write on ComponentInstance.ZIndex, mirroring
// ChangeBoundsCommand's before/after shape.
public sealed class ChangeZIndexCommand : ICommand
{
    private readonly ComponentInstance _instance;
    private readonly int _before;
    private readonly int _after;

    public ChangeZIndexCommand(ComponentInstance instance, int before, int after)
    {
        _instance = instance;
        _before = before;
        _after = after;
    }

    public void Apply() => _instance.ZIndex = _after;

    public void Undo() => _instance.ZIndex = _before;
}
