using D12Canvas.Model;

namespace D12Canvas.Pointer;

// What a pointer gesture may reach: the board, the viewport, the selection it is allowed to drive
// for the duration of the press, the gesture preview it publishes and commits, and the few
// canvas-level effects a gesture produces. Deliberately not a back-reference to the canvas, so a
// gesture needing more is a visible decision here.
internal interface IGestureContext
{
    Board? Board { get; }
    ZoomPanTracker ZoomPan { get; }
    SelectionSnapshot SelectionSnapshot { get; }

    (double X, double Y) ToBoardPoint(double containerX, double containerY);
    Guid EffectiveSelectionId(Guid entityId);
    bool IsSelected(Guid effectiveId);

    // The selection expanded through its groups to the component instances it moves.
    IReadOnlyList<ComponentInstance> SelectedInstances();

    void ReplaceSelection(IEnumerable<Guid> effectiveIds);
    void AddToSelection(Guid effectiveId);
    void RemoveFromSelection(Guid effectiveId);
    void SelectEdge(Guid edgeId);
    void ClearSelection();

    // The point rounded to the dominant grid line on each axis, or the point itself with
    // snap-to-grid off.
    (double X, double Y) SnapToGrid(double x, double y);

    void ShowMarquee(Bounds? boardBounds);

    // Replaces the gesture preview's bounds overrides. Board is not touched.
    void PublishPreview(IReadOnlyDictionary<Guid, Bounds> boundsOverrides);

    // Writes the last published preview back to Board as one history entry, leaving out every
    // instance whose previewed bounds equal its committed bounds.
    void CommitPreview();

    // Asks the instance to open its inline editor. Does nothing for an instance that is not
    // addressable, not mounted as its full component, or of a type that declines editing.
    void BeginInlineEdit(Guid instanceId);

    void OpenContextMenuAt(double containerX, double containerY);
}
