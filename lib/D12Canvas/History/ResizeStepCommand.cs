using D12Canvas.Model;

namespace D12Canvas.History;

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
