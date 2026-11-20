using D12Canvas.Model;

namespace D12Canvas.Pointer;

internal sealed class GesturePreview
{
    private Dictionary<Guid, Bounds> _boundsOverrides = new();
    private Dictionary<EdgeEnd, FloatingEndpoint> _movedEndpoints = new();

    public IReadOnlyDictionary<Guid, Bounds> BoundsOverrides => _boundsOverrides;

    public IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> MovedEndpoints => _movedEndpoints;

    public PendingEdge? PendingEdge { get; private set; }

    public IReadOnlyList<SnapGuide> Guides { get; private set; } = [];

    public Board? PendingFragment { get; private set; }

    public Bounds? SelectionFrame { get; private set; }

    public bool TryGetBounds(Guid instanceId, out Bounds bounds) =>
        _boundsOverrides.TryGetValue(instanceId, out bounds);

    public bool TryGetEndpoint(EdgeEnd end, out FloatingEndpoint endpoint) =>
        _movedEndpoints.TryGetValue(end, out endpoint);

    public void Publish(IReadOnlyDictionary<Guid, Bounds> boundsOverrides) =>
        _boundsOverrides = new Dictionary<Guid, Bounds>(boundsOverrides);

    public void PublishMovedEndpoints(
        IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> movedEndpoints
    ) => _movedEndpoints = new Dictionary<EdgeEnd, FloatingEndpoint>(movedEndpoints);

    public void PublishPendingEdge(PendingEdge pendingEdge) => PendingEdge = pendingEdge;

    public void PublishGuides(IReadOnlyList<SnapGuide> guides) => Guides = guides;

    public void PublishPendingFragment(Board? pendingFragment) => PendingFragment = pendingFragment;

    public void PublishSelectionFrame(Bounds? frame) => SelectionFrame = frame;

    public void Clear()
    {
        _boundsOverrides = new();
        _movedEndpoints = new();
        PendingEdge = null;
        Guides = [];
        PendingFragment = null;
        SelectionFrame = null;
    }
}

internal readonly record struct EdgeEnd(Guid EdgeId, bool IsSource);

internal sealed record PendingEdge(
    Guid? EdgeId,
    bool IsSource,
    IEdgeEndpoint Anchor,
    (double X, double Y) Point,
    Guid? DropTargetId = null
);
