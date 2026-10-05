using D12Canvas.Model;

namespace D12Canvas.Pointer;

// What a pointer gesture may reach: the board, the viewport, the selection it is allowed to drive
// for the duration of the press, and the few canvas-level effects a gesture produces. Deliberately
// not a back-reference to the canvas, so a gesture needing more is a visible decision here.
internal interface IGestureContext
{
    Board? Board { get; }
    ZoomPanTracker ZoomPan { get; }
    SelectionSnapshot SelectionSnapshot { get; }

    (double X, double Y) ToBoardPoint(double containerX, double containerY);
    Guid EffectiveSelectionId(Guid entityId);
    bool IsSelected(Guid effectiveId);
    bool IsMarqueeCandidate(ComponentInstance instance);

    void ReplaceSelection(IEnumerable<Guid> effectiveIds);
    void SelectEdge(Guid edgeId);
    void ClearSelection();
    void ShowMarquee(Bounds? boardBounds);
    void OpenContextMenuAt(double containerX, double containerY);
}
