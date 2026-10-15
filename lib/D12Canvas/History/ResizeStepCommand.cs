using D12Canvas.Model;

namespace D12Canvas.History;

// Backs Alt+Arrow keyboard resize - single-instance only, growing/shrinking one
// dimension per press with a stable anchor (the edge ResizeMath.Apply's direction leaves fixed).
// Like NudgeCommand, Extend grows this same command's accumulated delta in place so a held key's
// repeat keydowns coalesce into one undoable entry - but unlike a nudge's plain vector add, every
// Extend recomputes the resize from the ORIGINAL bounds via ResizeMath.Apply, which is what keeps
// the anchor edge exactly fixed across the whole burst instead of drifting through repeated
// incremental recomputation.
public sealed class ResizeStepCommand : ICommand
{
    private readonly ComponentInstance _target;
    private readonly ResizeDirection _direction;
    private readonly Bounds _before;
    private double _totalDeltaX;
    private double _totalDeltaY;

    public ResizeStepCommand(
        ComponentInstance target,
        ResizeDirection direction,
        double deltaX,
        double deltaY
    )
    {
        _target = target;
        _direction = direction;
        _before = target.Bounds;
        _totalDeltaX = deltaX;
        _totalDeltaY = deltaY;
    }

    public bool Matches(ComponentInstance target, ResizeDirection direction) =>
        ReferenceEquals(_target, target) && _direction == direction;

    public void Extend(double deltaX, double deltaY)
    {
        _totalDeltaX += deltaX;
        _totalDeltaY += deltaY;
        Apply();
    }

    public void Apply() =>
        _target.Bounds = ResizeMath.Apply(
            _before,
            _direction,
            _totalDeltaX,
            _totalDeltaY,
            ResizeMath.DefaultMinWidth,
            ResizeMath.DefaultMinHeight
        );

    public void Undo() => _target.Bounds = _before;
}
