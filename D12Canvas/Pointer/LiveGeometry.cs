using D12Canvas.Model;

namespace D12Canvas.Pointer;

// Where things are on screen now: each derivation consults the gesture preview before committed
// state. Board keeps the committed entry points for the same derivations, so a call site names
// which one it means; read Bounds off an instance for committed state and come here for what is
// on screen.
internal sealed class LiveGeometry(Board board, GesturePreview preview)
{
    public Bounds BoundsOf(ComponentInstance instance) =>
        preview.TryGetBounds(instance.Id, out var bounds) ? bounds : instance.Bounds;

    public (double X, double Y)? ResolveEndpoint(IEdgeEndpoint endpoint) =>
        board.ResolveEndpoint(endpoint, BoundsOf);

    public Bounds? GroupBounds(Group group) => board.GetBounds(group, BoundsOf);
}
