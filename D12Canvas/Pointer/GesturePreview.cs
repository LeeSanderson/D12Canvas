using D12Canvas.Model;

namespace D12Canvas.Pointer;

// What the active pointer gesture publishes while it runs: the bounds of its participants, keyed
// by instance id. Written only by that gesture, read only through live geometry, discarded on
// cancel and written back verbatim on commit. Board is never touched while it holds anything.
internal sealed class GesturePreview
{
    private Dictionary<Guid, Bounds> _boundsOverrides = new();

    public IReadOnlyDictionary<Guid, Bounds> BoundsOverrides => _boundsOverrides;

    public bool TryGetBounds(Guid instanceId, out Bounds bounds) =>
        _boundsOverrides.TryGetValue(instanceId, out bounds);

    public void Publish(IReadOnlyDictionary<Guid, Bounds> boundsOverrides) =>
        _boundsOverrides = new Dictionary<Guid, Bounds>(boundsOverrides);

    public void Clear() => _boundsOverrides = new();
}
