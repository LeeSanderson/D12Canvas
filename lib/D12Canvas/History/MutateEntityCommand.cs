using D12Canvas.Model;

namespace D12Canvas.History;

public sealed class MutateEntityCommand : ICommand
{
    private readonly ComponentInstance _instance;
    private readonly object _before;
    private readonly object _after;

    public MutateEntityCommand(ComponentInstance instance, object before, object after)
    {
        _instance = instance;
        _before = before;
        _after = after;
    }

    public void Apply() => _instance.Props = _after;

    public void Undo() => _instance.Props = _before;
}
