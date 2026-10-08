using D12Canvas.Model;

namespace D12Canvas.Pointer;

// What the active pointer gesture publishes while it runs: the bounds of its participants, keyed
// by instance id, the floating edge ends it carries, keyed by edge end, and at most one pending
// edge line. Written only by that gesture, read only through live geometry, discarded on cancel
// and written back verbatim on commit. Board is never touched while it holds anything.
internal sealed class GesturePreview
{
    private Dictionary<Guid, Bounds> _boundsOverrides = new();
    private Dictionary<EdgeEnd, FloatingEndpoint> _movedEndpoints = new();

    public IReadOnlyDictionary<Guid, Bounds> BoundsOverrides => _boundsOverrides;

    public IReadOnlyDictionary<EdgeEnd, FloatingEndpoint> MovedEndpoints => _movedEndpoints;

    public PendingEdge? PendingEdge { get; private set; }

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

    public void Clear()
    {
        _boundsOverrides = new();
        _movedEndpoints = new();
        PendingEdge = null;
    }
}

// One end of one edge: its source when IsSource is true, its target otherwise.
internal readonly record struct EdgeEnd(Guid EdgeId, bool IsSource);

// The line a connector drag draws, from the end that stays put to the pointer. EdgeId names the
// edge whose IsSource end is being carried, which the line stands in for while it is drawn; it is
// null while a new edge is being pulled from a port.
internal sealed record PendingEdge(
    Guid? EdgeId,
    bool IsSource,
    IEdgeEndpoint Anchor,
    (double X, double Y) Point
);
