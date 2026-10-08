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

    // The ancestor of the entity, or the entity itself, that is a direct member of the entered
    // group, or the outermost one when no group is entered.
    Guid EffectiveSelectionId(Guid entityId);

    // Whether the entity lies inside the entered group, which every entity does when none is.
    bool IsInScope(Guid entityId);

    // Whether the instance or edge takes part in primary-press hits and the marquee, which a locked
    // one does not. The markup marks a locked entity so the listener's press passes it by, and the
    // marquee sweeps only what does take part, so the two cannot disagree.
    bool HasHitRegion(Guid entityId);

    // Steps inside a group that is a direct member of the entered group, or a top-level group
    // when none is entered. The selection is left to the caller.
    void EnterGroup(Guid groupId);

    bool HasEnteredGroup { get; }

    // Steps out of the entered group as a primary press there would: until a pressed entity is
    // inside it, all the way for an edge, and until a press on empty canvas lies inside its bounds.
    void StepOutFor(PointerPress press);

    // The press's hit stack, each entity resolved as a press on it would select it, with the
    // selection box, its handles and duplicates left out. Empty when the press carried none.
    IReadOnlyList<HitStackEntry> HitStackOf(PointerPress press);

    // Both step out of the entered group as far as a press on the entry would, then replace the
    // selection with the entry or toggle it in or out.
    void SelectHitStackEntry(HitStackEntry entry);
    void ToggleHitStackEntry(HitStackEntry entry);

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

    // The dominant grid layer's spacing, in board units, or null with snap-to-grid off.
    double? GridSpacing { get; }

    bool ObjectSnapping { get; }

    // The bounds of every instance on screen that object snapping may match, leaving out the
    // excluded ones, which are what the gesture itself carries. Groups and edges are never
    // candidates.
    IReadOnlyList<Bounds> SnapCandidates(IReadOnlyCollection<Guid> excluded);

    // Replaces the gesture preview's guides. Board is not touched.
    void PublishGuides(IReadOnlyList<SnapGuide> guides);

    void ShowMarquee(Bounds? boardBounds);

    // Replaces the gesture preview's bounds overrides. Board is not touched.
    void PublishPreview(IReadOnlyDictionary<Guid, Bounds> boundsOverrides);

    // Replaces the gesture preview's moved floating endpoints. Board is not touched.
    void PublishMovedEndpoints(IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> movedEndpoints);

    // A copy of the selection exactly as duplicate would build it, every id fresh and stacked
    // above the board, or null when nothing copyable is selected. Board is not touched.
    Board? CopyOfSelection();

    // Replaces the gesture preview's pending fragment; null drops it. Board is not touched.
    void PublishPendingFragment(Board? pendingFragment);

    // Draws the selection's box at the frame rather than around its members. Board is not touched.
    void PublishSelectionFrame(Bounds frame);

    // Writes the last published preview back to Board as one history entry, leaving out every
    // instance whose previewed bounds equal its committed bounds and every edge end still where
    // it started. With a pending fragment it adds the fragment where the preview shows it
    // instead, makes it the selection and starts a duplicate run from it.
    void CommitPreview();

    // Replaces the gesture preview's pending edge line. Board is not touched.
    void PublishPendingEdge(PendingEdge pendingEdge);

    // Each of these is one history entry.
    void AddEdge(IEdgeEndpoint source, IEdgeEndpoint target);
    void ChangeEdgeEndpoint(Guid edgeId, bool isSource, IEdgeEndpoint endpoint);
    void AddEdgeLabel(Guid edgeId);

    // A copy of the port's instance beside it on the port's side, connected from that port, then
    // selected, focused and opened for editing when its type allows. One history entry.
    void QuickCreate(IEdgeEndpoint sourcePort);

    // Asks the edge's label to open its inline editor, as BeginInlineEdit does for an instance.
    void BeginLabelEdit(Guid edgeId);

    // Asks the instance to open its inline editor after the next render, panning it fully into
    // view first. Does nothing for an instance that is not addressable, locked, shown as a
    // placeholder, or of a type that declines editing.
    void BeginInlineEdit(Guid instanceId);

    // The object menu when the press hit an entity and something is selected, the canvas menu
    // otherwise.
    void OpenContextMenuAt(double containerX, double containerY, bool pressHitEntity);
}
