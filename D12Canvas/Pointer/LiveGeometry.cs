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

    public (double X, double Y)? ResolveEndpoint(IEdgeEndpoint endpoint, IEdgeEndpoint otherEnd) =>
        board.ResolveEndpoint(endpoint, otherEnd, BoundsOf);

    public (double X, double Y)? ResolveEnd(Edge edge, bool isSource) =>
        ResolveEndpoint(EndpointOf(edge, isSource), EndpointOf(edge, !isSource));

    public Bounds? GroupBounds(Group group) => board.GetBounds(group, BoundsOf);

    public RouteRequest? RouteRequestOf(Edge edge) =>
        RouteRequestBetween(
            EndpointOf(edge, isSource: true),
            EndpointOf(edge, isSource: false),
            edge.RoutingStyle
        );

    public RouteRequest? RouteRequestBetween(
        IEdgeEndpoint source,
        IEdgeEndpoint target,
        EdgeRouting style
    ) =>
        RouteEndOf(source, target) is { } sourceEnd && RouteEndOf(target, source) is { } targetEnd
            ? new RouteRequest(style, sourceEnd, targetEnd)
            : null;

    private RouteEnd? RouteEndOf(IEdgeEndpoint endpoint, IEdgeEndpoint otherEnd)
    {
        if (
            ResolveEndpoint(endpoint, otherEnd) is not { } point
            || SideOf(endpoint, otherEnd, point) is not { } side
        )
        {
            return null;
        }

        var shape =
            endpoint.ComponentId is { } id && board.GetComponent(id) is { } instance
                ? BoundsOf(instance)
                : (Bounds?)null;
        return new RouteEnd(point.X, point.Y, side, shape);
    }

    private PortId? SideOf(
        IEdgeEndpoint endpoint,
        IEdgeEndpoint otherEnd,
        (double X, double Y) resolvedAt
    ) =>
        endpoint switch
        {
            PortEndpoint port => port.PortId,
            CustomPortEndpoint custom => CustomPortSide(custom),
            AutoPortEndpoint auto => board.AutoPortSideOf(auto, otherEnd, BoundsOf),
            _ => ResolveEndpoint(otherEnd, endpoint) is { } other
                ? EdgeRouter.PseudoSide(resolvedAt, other)
                : null,
        };

    private PortId? CustomPortSide(CustomPortEndpoint custom) =>
        board
            .GetComponent(custom.ComponentId)
            ?.CustomPorts.Where(port => port.Id == custom.PortId)
            .Select(port => (PortId?)EdgeRouter.SideOfFraction(port.FractionX, port.FractionY))
            .FirstOrDefault();
}
