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

    public IEdgeEndpoint EndpointOf(Edge edge, bool isSource) =>
        preview.TryGetEndpoint(new EdgeEnd(edge.Id, isSource), out var moved) ? moved
        : isSource ? edge.Source
        : edge.Target;

    public (double X, double Y)? ResolveEndpoint(IEdgeEndpoint endpoint) =>
        board.ResolveEndpoint(endpoint, BoundsOf);

    public (double X, double Y)? ResolveEnd(Edge edge, bool isSource) =>
        ResolveEndpoint(EndpointOf(edge, isSource));

    public Bounds? GroupBounds(Group group) => board.GetBounds(group, BoundsOf);
}
