namespace D12Canvas.Pointer;

// A primary press on an author's own content. The browser keeps the press: nothing is captured or
// tracked, so no move or release ever arrives and the press ends as soon as it begins. Its one
// effect is selecting the enclosing instance if the selection does not already hold it, never
// removing anything, so a click into a control does not break up the selection around it.
internal sealed class NativeGesture(PointerPress press, IGestureContext context)
    : PointerGesture(press, context)
{
    public override bool HoldsPress => false;

    protected override void OnPress()
    {
        if (Press.EntityId is not { } entityId)
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
