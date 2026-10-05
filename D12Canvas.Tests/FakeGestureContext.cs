using D12Canvas.Model;
using D12Canvas.Pointer;

namespace D12Canvas.Tests;

// The canvas-side seam a pointer gesture sees, replaced by plain state so a gesture can be driven
// with presses, moves and a release and asked what it published and what it committed.
internal sealed class FakeGestureContext : IGestureContext
{
    public FakeGestureContext(Board board, ZoomPanTracker? zoomPan = null)
    {
        Board = board;
        ZoomPan = zoomPan ?? new ZoomPanTracker();
        ZoomPan.SetContainerSize(800, 600);
        SelectionSnapshot = new SelectionSnapshot([], null);
    }

    public Board? Board { get; }
    public ZoomPanTracker ZoomPan { get; }
    public SelectionSnapshot SelectionSnapshot { get; set; }

    public HashSet<Guid> SelectedInstanceIds { get; } = new();
    public Guid? SelectedEdgeId { get; private set; }
    public Bounds? Marquee { get; private set; }
    public List<(double X, double Y)> ContextMenuOpenings { get; } = new();
    public Func<Guid, Guid> EffectiveId { get; set; } = id => id;
    public Func<ComponentInstance, bool> MarqueeCandidate { get; set; } = _ => true;

    public (double X, double Y) ToBoardPoint(double containerX, double containerY) =>
        ((containerX - ZoomPan.PanX) / ZoomPan.Scale, (containerY - ZoomPan.PanY) / ZoomPan.Scale);

    public Guid EffectiveSelectionId(Guid entityId) => EffectiveId(entityId);

    public bool IsSelected(Guid effectiveId) => SelectedInstanceIds.Contains(effectiveId);

    public bool IsMarqueeCandidate(ComponentInstance instance) => MarqueeCandidate(instance);

    public void ReplaceSelection(IEnumerable<Guid> effectiveIds)
    {
        SelectedInstanceIds.Clear();
        SelectedInstanceIds.UnionWith(effectiveIds);
        SelectedEdgeId = null;
    }

    public void SelectEdge(Guid edgeId)
    {
        SelectedInstanceIds.Clear();
        SelectedEdgeId = edgeId;
    }

    public void ClearSelection()
    {
        SelectedInstanceIds.Clear();
        SelectedEdgeId = null;
    }

    public void ShowMarquee(Bounds? boardBounds) => Marquee = boardBounds;

    public void OpenContextMenuAt(double containerX, double containerY) =>
        ContextMenuOpenings.Add((containerX, containerY));
}
