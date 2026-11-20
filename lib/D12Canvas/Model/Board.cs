namespace D12Canvas.Model;

public sealed class Board
{
    private readonly Dictionary<Guid, ComponentInstance> _components = new();
    private readonly Dictionary<Guid, Group> _groups = new();
    private readonly Dictionary<Guid, Edge> _edges = new();
    private readonly Dictionary<string, Asset> _assets = new(StringComparer.Ordinal);

    public IReadOnlyCollection<ComponentInstance> Components => _components.Values;
    public IReadOnlyCollection<Group> Groups => _groups.Values;
    public IReadOnlyCollection<Edge> Edges => _edges.Values;
    public IReadOnlyCollection<Asset> Assets => _assets.Values;

    public string AddAsset(byte[] data, string mimeType)
    {
        var id = Asset.IdFor(data);
        _assets.TryAdd(id, new Asset(id, mimeType, data.ToArray()));
        return AssetReference.Format(id);
    }

    internal void AddAsset(Asset asset) => _assets.TryAdd(asset.Id, asset);

    public Asset? GetAsset(string id) => _assets.TryGetValue(id, out var asset) ? asset : null;

    internal IEnumerable<ComponentInstance> InstancesIncludingEdgeLabels() =>
        _components.Values.Concat(
            _edges.Values.Select(edge => edge.Label).OfType<ComponentInstance>()
        );

    public void AddComponent(ComponentInstance instance) => _components.Add(instance.Id, instance);

    public void RemoveComponent(Guid id) => _components.Remove(id);

    public ComponentInstance? GetComponent(Guid id) =>
        _components.TryGetValue(id, out var instance) ? instance : null;

    public void AddGroup(Group group) => _groups.Add(group.Id, group);

    public void RemoveGroup(Guid id) => _groups.Remove(id);

    public Group? GetGroup(Guid id) => _groups.TryGetValue(id, out var group) ? group : null;

    public void AddEdge(Edge edge) => _edges.Add(edge.Id, edge);

    public void RemoveEdge(Guid id) => _edges.Remove(id);

    public Edge? GetEdge(Guid id) => _edges.TryGetValue(id, out var edge) ? edge : null;

    public (double X, double Y)? ResolveEndpoint(IEdgeEndpoint endpoint, IEdgeEndpoint otherEnd) =>
        ResolveEndpoint(endpoint, otherEnd, CommittedBounds);

    public (double X, double Y)? ResolveEnd(Edge edge, bool isSource) =>
        isSource
            ? ResolveEndpoint(edge.Source, edge.Target)
            : ResolveEndpoint(edge.Target, edge.Source);

    internal (double X, double Y)? ResolveEndpoint(
        IEdgeEndpoint endpoint,
        IEdgeEndpoint otherEnd,
        Func<ComponentInstance, Bounds> boundsOf
    ) =>
        endpoint switch
        {
            AutoPortEndpoint auto => ResolveAutoPort(auto, otherEnd, boundsOf),
            _ => ReferencePoint(endpoint, boundsOf),
        };

    private (double X, double Y)? ReferencePoint(
        IEdgeEndpoint endpoint,
        Func<ComponentInstance, Bounds> boundsOf
    ) =>
        endpoint switch
        {
            PortEndpoint port => ResolvePort(port, boundsOf),
            CustomPortEndpoint custom => ResolveCustomPort(custom, boundsOf),
            FloatingEndpoint floating => (floating.X, floating.Y),
            AutoPortEndpoint auto => GetComponent(auto.ComponentId) is { } instance
                ? boundsOf(instance).PointAtFraction(0.5, 0.5)
                : null,
            _ => null,
        };

    private (double X, double Y)? ResolveAutoPort(
        AutoPortEndpoint auto,
        IEdgeEndpoint otherEnd,
        Func<ComponentInstance, Bounds> boundsOf
    ) =>
        AutoPortSideOf(auto, otherEnd, boundsOf) is { } side
            ? ResolvePort(new PortEndpoint(auto.ComponentId, side), boundsOf)
            : null;

    internal PortId? AutoPortSideOf(
        AutoPortEndpoint auto,
        IEdgeEndpoint otherEnd,
        Func<ComponentInstance, Bounds> boundsOf
    ) =>
        GetComponent(auto.ComponentId) is { } instance
        && ReferencePoint(otherEnd, boundsOf) is { } aimedAt
            ? AutoPortSide.Facing(boundsOf(instance), aimedAt)
            : null;

    private static Bounds CommittedBounds(ComponentInstance instance) => instance.Bounds;

    private (double X, double Y)? ResolvePort(
        PortEndpoint port,
        Func<ComponentInstance, Bounds> boundsOf
    )
    {
        var instance = GetComponent(port.ComponentId);
        if (instance is null)
        {
            return null;
        }

        var (fractionX, fractionY) = StandardPorts.FractionOf(port.PortId);
        return boundsOf(instance).PointAtFraction(fractionX, fractionY);
    }

    private (double X, double Y)? ResolveCustomPort(
        CustomPortEndpoint endpoint,
        Func<ComponentInstance, Bounds> boundsOf
    )
    {
        var instance = GetComponent(endpoint.ComponentId);
        if (instance is null)
        {
            return null;
        }

        foreach (var port in instance.CustomPorts)
        {
            if (port.Id == endpoint.PortId)
            {
                return boundsOf(instance).PointAtFraction(port.FractionX, port.FractionY);
            }
        }

        return null;
    }

    public IEnumerable<(IEdgeEndpoint Endpoint, double FractionX, double FractionY)> AllPorts(
        ComponentInstance instance
    )
    {
        foreach (var portId in StandardPorts.All)
        {
            var (fractionX, fractionY) = StandardPorts.FractionOf(portId);
            yield return (new PortEndpoint(instance.Id, portId), fractionX, fractionY);
        }

        foreach (var port in instance.CustomPorts)
        {
            yield return (
                new CustomPortEndpoint(instance.Id, port.Id),
                port.FractionX,
                port.FractionY
            );
        }
    }

    public (Guid EdgeId, bool IsSource)? FindEdgeAttachedTo(IEdgeEndpoint endpoint)
    {
        foreach (var edge in _edges.Values)
        {
            if (edge.Source.Equals(endpoint))
            {
                return (edge.Id, true);
            }

            if (edge.Target.Equals(endpoint))
            {
                return (edge.Id, false);
            }
        }

        return null;
    }

    public ComponentInstance? FindEdgeLabel(Guid instanceId) =>
        _edges.Values.Select(edge => edge.Label).FirstOrDefault(label => label?.Id == instanceId);

    public Bounds? GetBounds(Group group) => GetBounds(group, CommittedBounds);

    internal Bounds? GetBounds(Group group, Func<ComponentInstance, Bounds> boundsOf) =>
        Bounds.Union(
            group
                .MemberIds.Select(memberId => MemberBounds(memberId, boundsOf))
                .Where(b => b.HasValue)
                .Select(b => b!.Value)
        );

    private Bounds? MemberBounds(Guid memberId, Func<ComponentInstance, Bounds> boundsOf) =>
        GetComponent(memberId) is { } instance ? boundsOf(instance)
        : GetGroup(memberId) is { } nested ? GetBounds(nested, boundsOf)
        : null;

    public bool IsLocked(Guid id)
    {
        if (GetComponent(id) is { } instance)
        {
            return instance.Locked;
        }

        if (GetEdge(id) is { } edge)
        {
            return edge.Locked;
        }

        if (GetGroup(id) is not { } group)
        {
            return false;
        }

        var resolving = group
            .MemberIds.Where(memberId =>
                GetComponent(memberId) is not null || GetGroup(memberId) is not null
            )
            .ToList();
        return resolving.Count > 0 && resolving.All(IsLocked);
    }

    public Group? FindContainingGroup(Guid memberId)
    {
        var parent = FindParentGroup(memberId);
        if (parent is null)
        {
            return null;
        }

        while (true)
        {
            var grandparent = FindParentGroup(parent.Id);
            if (grandparent is null)
            {
                return parent;
            }

            parent = grandparent;
        }
    }

    public Group? FindParentGroup(Guid memberId) =>
        _groups.Values.FirstOrDefault(group => group.MemberIds.Contains(memberId));

    internal Bounds? ContentExtent() => ExtentOf(_components.Values, _edges.Values);

    internal Bounds? ExtentOf(IEnumerable<ComponentInstance> instances, IEnumerable<Edge> edges) =>
        Bounds.Union(
            instances
                .Select(instance => instance.Bounds)
                .Concat(
                    edges
                        .SelectMany(edge =>
                            new[] { ResolveEnd(edge, true), ResolveEnd(edge, false) }
                        )
                        .OfType<(double X, double Y)>()
                        .Select(point => new Bounds(point.X, point.Y, 0, 0))
                )
        );

    public IReadOnlyCollection<ComponentInstance> GetVisible(Bounds viewport, double overscan = 0)
    {
        var expandedViewport = viewport.ExpandedBy(overscan);

        return _components
            .Values.Where(instance => expandedViewport.Intersects(instance.Bounds))
            .ToList();
    }

    public IReadOnlyCollection<Group> GetVisibleGroups(Bounds viewport, double overscan = 0)
    {
        var expandedViewport = viewport.ExpandedBy(overscan);

        return _groups
            .Values.Where(group => FindContainingGroup(group.Id) is null)
            .Where(group => GetBounds(group) is { } bounds && expandedViewport.Intersects(bounds))
            .ToList();
    }

    public int NextZIndex() =>
        _components.Count == 0 ? 0 : _components.Values.Max(c => c.ZIndex) + 1;

    public int PreviousZIndex() =>
        _components.Count == 0 ? 0 : _components.Values.Min(c => c.ZIndex) - 1;

    public int? ZIndexAbove(int zIndex)
    {
        var higher = _components.Values.Where(c => c.ZIndex > zIndex).ToList();
        return higher.Count == 0 ? null : higher.Min(c => c.ZIndex);
    }

    public int? ZIndexBelow(int zIndex)
    {
        var lower = _components.Values.Where(c => c.ZIndex < zIndex).ToList();
        return lower.Count == 0 ? null : lower.Max(c => c.ZIndex);
    }
}
