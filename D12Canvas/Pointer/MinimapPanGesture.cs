namespace D12Canvas.Pointer;

// A press on the minimap. Its points are minimap pixels, which the minimap's own mapping turns
// into board points; the minimap holds that mapping still for the whole press, so the point under
// the pointer never slides as the viewport moves. A drag that starts inside the viewport rect
// carries the rect from where it was grabbed; one that starts outside it centres the viewport on
// the board point under the pointer. Either way it is unanimated, and a click flies the viewport's
// centre to the pressed point. Zoom is never touched. Any other viewport change re-anchors, so the
// pointer carries on from where that change left the viewport and the two movements add.
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
