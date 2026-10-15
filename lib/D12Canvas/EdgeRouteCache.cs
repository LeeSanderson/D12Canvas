using D12Canvas.Model;

namespace D12Canvas;

// Each edge's last route, kept until its request changes. Pan and zoom change no request, so they
// route nothing.
internal sealed class EdgeRouteCache
{
    private readonly Dictionary<Guid, EdgeRoute> _routes = new();

    public int RoutesComputed { get; private set; }

    public EdgeRoute RouteFor(Guid edgeId, RouteRequest request)
    {
        if (_routes.TryGetValue(edgeId, out var cached) && cached.Request == request)
        {
            return cached;
        }

        var route = EdgeRouter.Route(request);
        RoutesComputed++;
        _routes[edgeId] = route;
        return route;
    }

    public void RetainOnly(IReadOnlyCollection<Edge> edges)
    {
        if (_routes.Count <= edges.Count)
        {
            return;
        }

        var kept = edges.Select(edge => edge.Id).ToHashSet();
        foreach (var stale in _routes.Keys.Where(id => !kept.Contains(id)).ToList())
        {
            _routes.Remove(stale);
        }
    }
}
