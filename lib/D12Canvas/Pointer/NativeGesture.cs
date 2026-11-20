namespace D12Canvas.Pointer;

internal sealed class NativeGesture(PointerPress press, IGestureContext context)
    : PointerGesture(press, context)
{
    public override bool HoldsPress => false;

    protected override bool HasActivePhase => false;

    protected override void OnPress()
    {
        if (Press.EntityId is not { } entityId || Context.Board?.GetEdge(entityId) is not null)
        {
            return;
        }

        var effectiveId = Context.EffectiveSelectionId(entityId);
        if (!Context.IsSelected(effectiveId))
        {
            Context.AddToSelection(effectiveId);
        }
    }

    protected override void OnMove(PointerMove move) { }

    protected override void OnRelease(PointerRelease release) { }

    protected override void OnClick(PointerRelease release) { }
}
