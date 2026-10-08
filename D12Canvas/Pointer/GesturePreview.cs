using D12Canvas.Model;

namespace D12Canvas.Pointer;

// What the active pointer gesture publishes while it runs: the bounds of its participants, keyed
// by instance id, the floating edge ends it carries, keyed by edge end, at most one pending edge
// line, the guides drawn along what object snapping matched, and the pending fragment a clone drag
// adds at release, whose entities are not in Board yet. Written only by that gesture,
// read only through live geometry, discarded on cancel and written back verbatim on commit. Board
// is never touched while it holds anything.
internal sealed class GesturePreview
{
    private Dictionary<Guid, Bounds> _boundsOverrides = new();
    private Dictionary<EdgeEnd, FloatingEndpoint> _movedEndpoints = new();

    public IReadOnlyDictionary<Guid, Bounds> BoundsOverrides => _boundsOverrides;

    public IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> MovedEndpoints => _movedEndpoints;

    public PendingEdge? PendingEdge { get; private set; }

    public IReadOnlyList<SnapGuide> Guides { get; private set; } = [];

    public Board? PendingFragment { get; private set; }

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

    public void Clear()
    {
        _boundsOverrides = new();
        _movedEndpoints = new();
        PendingEdge = null;
        Guides = [];
        PendingFragment = null;
    }
}

// One end of one edge: its source when IsSource is true, its target otherwise.
internal readonly record struct EdgeEnd(Guid EdgeId, bool IsSource);

// The line a connector drag draws, from the end that stays put to the pointer. EdgeId names the
// edge whose IsSource end is being carried, which the line stands in for while it is drawn; it is
// null while a new edge is being pulled from a port. DropTargetId is the shape under the pointer
// that a release would attach to, which shows its ports; never the shape the anchor is on.
internal sealed record PendingEdge(
    Guid? EdgeId,
    bool IsSource,
    IEdgeEndpoint Anchor,
    (double X, double Y) Point,
    Guid? DropTargetId = null
);
