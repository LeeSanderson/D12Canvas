namespace D12Canvas.Pointer;

internal sealed class SelectEdgeGesture(PointerPress press, IGestureContext context)
    : PointerGesture(press, context)
{
    protected override bool HasActivePhase => false;

    protected override void OnMove(PointerMove move) { }

    protected override void OnRelease(PointerRelease release) { }

    protected override void OnClick(PointerRelease release)
    {
        if (Press.EntityId is not { } edgeId || TrySelectNextInHitStack())
        {
            return;
        }

        if (Press.PressCount == 1)
        {
            if (Press.ShiftKey)
            {
                Context.ToggleEdge(edgeId);
            }
            else
            {
                Context.SelectEdge(edgeId);
            }
        }
        else if (Press.Role == HitRole.EdgeLabel)
        {
            Context.BeginLabelEdit(edgeId);
        }
        else
        {
            Context.AddEdgeLabel(edgeId);
        }
    }
}
