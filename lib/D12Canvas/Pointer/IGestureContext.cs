using D12Canvas.Model;

namespace D12Canvas.Pointer;

internal interface IGestureContext
{
    Board? Board { get; }
    ZoomPanTracker ZoomPan { get; }
    SelectionSnapshot SelectionSnapshot { get; }

    (double X, double Y) ToBoardPoint(double containerX, double containerY);

    void CentreViewportOn(double boardX, double boardY, bool animated);

    Guid EffectiveSelectionId(Guid entityId);

    bool IsInScope(Guid entityId);

    bool HasHitRegion(Guid entityId);

    void EnterGroup(Guid groupId);

    bool HasEnteredGroup { get; }

    void StepOutFor(PointerPress press);

    IReadOnlyList<HitStackEntry> HitStackOf(PointerPress press);

    void SelectHitStackEntry(HitStackEntry entry);
    void ToggleHitStackEntry(HitStackEntry entry);

    bool IsSelected(Guid effectiveId);
    bool IsEdgeSelected(Guid edgeId);

    IReadOnlyList<ComponentInstance> SelectedInstances();

    IReadOnlyList<Edge> SelectedEdges();

    void ReplaceSelection(IEnumerable<Guid> effectiveIds, IEnumerable<Guid> edgeIds);

    void AddToSelection(Guid effectiveId);
    void RemoveFromSelection(Guid effectiveId);

    void SelectEdge(Guid edgeId);

    void ToggleEdge(Guid edgeId);
    void ClearSelection();

    double? GridSpacing { get; }

    bool ObjectSnapping { get; }

    IReadOnlyList<Bounds> SnapCandidates(IReadOnlyCollection<Guid> excluded);

    void PublishGuides(IReadOnlyList<SnapGuide> guides);

    void ShowMarquee(Bounds? boardBounds);

    void PublishPreview(IReadOnlyDictionary<Guid, Bounds> boundsOverrides);

    void PublishMovedEndpoints(IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> movedEndpoints);

    Board? CopyOfSelection();

    void PublishPendingFragment(Board? pendingFragment);

    void PublishSelectionFrame(Bounds frame);

    void CommitPreview();

    void PublishPendingEdge(PendingEdge pendingEdge);

    void AddEdge(IEdgeEndpoint source, IEdgeEndpoint target);
    void ChangeEdgeEndpoint(Guid edgeId, bool isSource, IEdgeEndpoint endpoint);
    void AddEdgeLabel(Guid edgeId);

    void QuickCreate(IEdgeEndpoint sourcePort);

    void BeginLabelEdit(Guid edgeId);

    void BeginInlineEdit(Guid instanceId);

    void OpenContextMenuAt(PointerPress press);
}
