namespace D12Canvas.Pointer;

internal sealed class MinimapPanGesture : PointerGesture
{
    private readonly Func<double, double, (double X, double Y)> _toBoardPoint;
    private (double X, double Y) _offset;

    public MinimapPanGesture(
        PointerPress press,
        IGestureContext context,
        Func<double, double, (double X, double Y)> toBoardPoint
    )
        : base(press, context)
    {
        _toBoardPoint = toBoardPoint;
    }

    protected override void OnPress()
    {
        var (x, y) = _toBoardPoint(Press.X, Press.Y);
        var viewport = Context.ZoomPan.Viewport;
        if (viewport.Contains(x, y))
        {
            _offset = (viewport.X + viewport.Width / 2 - x, viewport.Y + viewport.Height / 2 - y);
        }
    }

    protected override void OnMove(PointerMove move)
    {
        var (x, y) = _toBoardPoint(move.X, move.Y);
        Context.CentreViewportOn(x + _offset.X, y + _offset.Y, animated: false);
    }

    protected override void OnViewportMoved(PointerMove pointer)
    {
        var (x, y) = _toBoardPoint(pointer.X, pointer.Y);
        var viewport = Context.ZoomPan.Viewport;
        _offset = (viewport.X + viewport.Width / 2 - x, viewport.Y + viewport.Height / 2 - y);
    }

    protected override void OnRelease(PointerRelease release) { }

    protected override void OnClick(PointerRelease release)
    {
        var (x, y) = _toBoardPoint(Press.X, Press.Y);
        Context.CentreViewportOn(x, y, animated: true);
    }
}
