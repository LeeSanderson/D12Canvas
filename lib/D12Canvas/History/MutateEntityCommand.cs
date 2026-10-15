using D12Canvas.Model;

namespace D12Canvas.History;

// A generic swap for a component instance's opaque, boxed Props - because Props is only ever
// resolved via a registry lookup, no per-component-type undo logic is possible or needed here,
// unlike ChangeBoundsCommand which knows Bounds's own shape.
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
