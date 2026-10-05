namespace D12Canvas.Pointer;

// A primary press on an edge's line or on its label. It has no active phase: a release below the
// threshold selects the edge, and a press that crossed the threshold is abandoned, as a native
// button is, rather than selecting on a release far from what was pressed. A double-press on the
// line adds a label to an edge that has none, and on the label opens the label's editor.
internal sealed class SelectEdgeGesture(PointerPress press, IGestureContext context)
    : PointerGesture(press, context)
{
    protected override void OnMove(PointerMove move) { }

    protected override void OnRelease(PointerRelease release) { }

    protected override void OnClick(PointerRelease release)
    {
        if (Press.EntityId is not { } edgeId)
        {
            return;
        }

        if (Press.PressCount == 1)
        {
            Context.SelectEdge(edgeId);
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
