namespace D12Canvas.Pointer;

// The secondary and middle buttons pan whatever they land on. The pan holds the board point that
// was under the press under the pointer, so a dropped frame costs nothing and a wheel zoom about
// the pointer composes with it untouched. Any other viewport change re-anchors to the board point
// now under the pointer, so the viewport's movement and the hand's add up. A secondary release that
// never crossed the threshold is the context menu: it resolves the selection at that moment, so a
// right-drag pan never wipes the selection as a side effect, and opens the menu at the press point.
// A middle click does nothing. The viewport is never restored on cancel.
internal sealed class PanGesture : PointerGesture
{
    private (double X, double Y) _grabbed;

    public PanGesture(PointerPress press, IGestureContext context)
        : base(press, context)
    {
        _grabbed = context.ToBoardPoint(press.X, press.Y);
    }

    protected override void OnMove(PointerMove move) =>
        Context.ZoomPan.SetPanPosition(
            move.X - _grabbed.X * Context.ZoomPan.Scale,
            move.Y - _grabbed.Y * Context.ZoomPan.Scale
        );

    protected override void OnViewportMoved(PointerMove pointer) =>
        _grabbed = Context.ToBoardPoint(pointer.X, pointer.Y);

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
                if (Press.EntityId is { } edgeId && !Context.IsEdgeSelected(edgeId))
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
                        Context.ReplaceSelection([effectiveId], []);
                    }
                }
                break;
        }
    }
}
