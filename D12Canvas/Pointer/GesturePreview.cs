using D12Canvas.Model;

namespace D12Canvas.Pointer;

// What the active pointer gesture publishes while it runs: the bounds of its participants, keyed
// by instance id, and at most one pending edge line. Written only by that gesture, read only
// through live geometry, discarded on cancel and written back verbatim on commit. Board is never
// touched while it holds anything.
internal sealed class GesturePreview
{
    private Dictionary<Guid, Bounds> _boundsOverrides = new();

    public IReadOnlyDictionary<Guid, Bounds> BoundsOverrides => _boundsOverrides;

    public PendingEdge? PendingEdge { get; private set; }

    public bool TryGetBounds(Guid instanceId, out Bounds bounds) =>
        _boundsOverrides.TryGetValue(instanceId, out bounds);

    public void Publish(IReadOnlyDictionary<Guid, Bounds> boundsOverrides) =>
        _boundsOverrides = new Dictionary<Guid, Bounds>(boundsOverrides);

    public void PublishPendingEdge(PendingEdge pendingEdge) => PendingEdge = pendingEdge;

    public void Clear()
    {
        _boundsOverrides = new();
        PendingEdge = null;
    }
}

// The line a connector drag draws, from the end that stays put to the pointer. EdgeId names the
// edge whose IsSource end is being carried, which the line stands in for while it is drawn; it is
// null while a new edge is being pulled from a port.
internal sealed record PendingEdge(
    Guid? EdgeId,
    bool IsSource,
    IEdgeEndpoint Anchor,
    (double X, double Y) Point
);
