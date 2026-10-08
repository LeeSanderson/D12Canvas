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
    bool IsEdgeSelected(Guid edgeId);

    // The selection expanded through its groups to the component instances it moves.
    IReadOnlyList<ComponentInstance> SelectedInstances();

    IReadOnlyList<Edge> SelectedEdges();

    // Replaces both selection sets.
    void ReplaceSelection(IEnumerable<Guid> effectiveIds, IEnumerable<Guid> edgeIds);

    // Both leave the selected edges as they are.
    void AddToSelection(Guid effectiveId);
    void RemoveFromSelection(Guid effectiveId);

    // Replaces the whole selection with the one edge.
    void SelectEdge(Guid edgeId);

    // Adds the edge or takes it out, leaving the rest of the selection as it is.
    void ToggleEdge(Guid edgeId);
    void ClearSelection();

    // The point rounded to the dominant grid line on each axis, or the point itself with
    // snap-to-grid off.
    (double X, double Y) SnapToGrid(double x, double y);

    // The spacing SnapToGrid rounds to, in board units, or null with snap-to-grid off.
    double? GridSpacing { get; }

    void ShowMarquee(Bounds? boardBounds);

    // Replaces the gesture preview's bounds overrides. Board is not touched.
    void PublishPreview(IReadOnlyDictionary<Guid, Bounds> boundsOverrides);

    // Replaces the gesture preview's moved floating endpoints. Board is not touched.
    void PublishMovedEndpoints(IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> movedEndpoints);

    // Writes the last published preview back to Board as one history entry, leaving out every
    // instance whose previewed bounds equal its committed bounds and every edge end still where
    // it started.
    void CommitPreview();

    // Replaces the gesture preview's pending edge line. Board is not touched.
    void PublishPendingEdge(PendingEdge pendingEdge);

    // Each of these is one history entry.
    void AddEdge(IEdgeEndpoint source, IEdgeEndpoint target);
    void ChangeEdgeEndpoint(Guid edgeId, bool isSource, IEdgeEndpoint endpoint);
    void AddCustomPort(Guid instanceId, PortDef port);
    void AddEdgeLabel(Guid edgeId);

    // Asks the edge's label to open its inline editor, as BeginInlineEdit does for an instance.
    void BeginLabelEdit(Guid edgeId);

    // Asks the instance to open its inline editor. Does nothing for an instance that is not
    // addressable, not mounted as its full component, or of a type that declines editing.
    void BeginInlineEdit(Guid instanceId);

    void OpenContextMenuAt(double containerX, double containerY);
}
