namespace D12Canvas.Pointer;

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

        Context.StepOutFor(Press);
        ResolveSelectionForMenu();
        Context.OpenContextMenuAt(Press);
    }

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
