namespace D12Canvas.Pointer;

// The secondary and middle buttons pan whatever they land on. The pan is press-anchored,
// panOrigin + (current - press) in screen pixels, so a dropped frame costs nothing and the
// viewport never needs an anchor reset. A secondary release that never crossed the threshold is
// the context menu: it resolves the selection at that moment, so a right-drag pan never wipes
// the selection as a side effect, and opens the menu at the press point. A middle click does
// nothing. The viewport is never restored on cancel.
internal sealed class PanGesture : PointerGesture
{
    private readonly double _panXAtPress;
    private readonly double _panYAtPress;

    public PanGesture(PointerPress press, IGestureContext context)
        : base(press, context)
    {
        _panXAtPress = context.ZoomPan.PanX;
        _panYAtPress = context.ZoomPan.PanY;
    }

    protected override void OnMove(PointerMove move) =>
        Context.ZoomPan.SetPanPosition(
            _panXAtPress + (move.X - Press.X),
            _panYAtPress + (move.Y - Press.Y)
        );

    protected override void OnRelease(PointerRelease release) { }

    protected override void OnClick(PointerRelease release)
    {
        if (Press.Button != PointerPress.SecondaryButton)
        {
            return;
        }

        ResolveSelectionForMenu();
        Context.OpenContextMenuAt(Press.X, Press.Y);
    }

    // One predicate: does the pressed entity, resolved outward to its group, belong to the current
    // selection? Yes preserves, no replaces, no entity at all clears.
    private void ResolveSelectionForMenu()
    {
        switch (Press.Role)
        {
            case HitRole.Edge:
            case HitRole.EdgeLabel:
            case HitRole.EdgeEndpoint:
                if (Press.EntityId is { } edgeId)
                {
                    Context.SelectEdge(edgeId);
                }
                break;
            case HitRole.Canvas:
                Context.ClearSelection();
                break;
            default:
                if (Press.EntityId is { } entityId)
                {
                    var effectiveId = Context.EffectiveSelectionId(entityId);
                    if (!Context.IsSelected(effectiveId))
                    {
                        Context.ReplaceSelection([effectiveId]);
                    }
                }
                break;
        }
    }
}
