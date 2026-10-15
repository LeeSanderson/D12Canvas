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

    // The only public way bytes enter a board. Content-addressed and idempotent: the same bytes
    // added twice are one asset, and the return value is the reference string a declared props
    // property stores. Outside history, because creating the instance that refers to the asset is
    // the undoable act; nothing removes an asset during a session, so undo never needs the bytes.
    public string AddAsset(byte[] data, string mimeType)
    {
        var id = Asset.IdFor(data);
        _assets.TryAdd(id, new Asset(id, mimeType, data.ToArray()));
        return AssetReference.Format(id);
    }

    internal void AddAsset(Asset asset) => _assets.TryAdd(asset.Id, asset);

    public Asset? GetAsset(string id) => _assets.TryGetValue(id, out var asset) ? asset : null;

    // An edge's label is a full ComponentInstance that lives on its edge rather than in
    // Components, so anything that must see every instance on the board walks this instead.
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

    // An endpoint's committed board-space point. A PortEndpoint/CustomPortEndpoint resolves from
    // its referenced instance's Bounds rather than stored - what lets an attached edge track
    // move/resize for free; a FloatingEndpoint resolves to its own fixed point, tracking nothing.
    // An AutoPortEndpoint needs the edge's other end to choose its side, so every endpoint is
    // resolved as one of a pair. Null when a referenced instance or custom port no longer exists.
    public (double X, double Y)? ResolveEndpoint(IEdgeEndpoint endpoint, IEdgeEndpoint otherEnd) =>
        ResolveEndpoint(endpoint, otherEnd, CommittedBounds);

    public (double X, double Y)? ResolveEnd(Edge edge, bool isSource) =>
        isSource
            ? ResolveEndpoint(edge.Source, edge.Target)
            : ResolveEndpoint(edge.Target, edge.Source);

    // The one implementation behind both the committed entry point above and live geometry's,
    // which differ only in where an instance's bounds come from.
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

    // The standard port an auto endpoint currently resolves to, or null when either end has
    // nothing to resolve against.
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

    // Every port an instance exposes, standard and custom alike, each already paired with the
    // IEdgeEndpoint it resolves to - the ordered list DiagramCanvas's keyboard
    // connector-attachment gesture cycles through (Space, once a port is being picked).
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

    // Does any edge end sit pinned to this exact port (standard or custom)? Tells "start a new
    // edge" apart from "carry this edge's existing endpoint" (see DragEdgeEndGesture). Multiple
    // edges sharing the same port pick whichever is found first. An auto end is never found here:
    // it names no port, and the one it resolves to changes as the other end moves.
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

    // Resolves an edge label's live ComponentInstance by its own id - used by
    // DiagramCanvas.CommitPropsChange to find the right object to mutate when a label's inline
    // text edit commits. A label has no Board.Components entry of its own (it's embedded directly
    // on its owning Edge instead), so it needs this separate lookup rather than GetComponent.
    public ComponentInstance? FindEdgeLabel(Guid instanceId) =>
        _edges.Values.Select(edge => edge.Label).FirstOrDefault(label => label?.Id == instanceId);

    // A Group's bounds are never stored - only ever resolved on demand from its members' committed
    // bounds, which may themselves be component instances or nested groups. A member id that no
    // longer resolves to anything (deleted out from under the group) is skipped rather than failing
    // the whole computation.
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

    // An instance or an edge by its own flag; a group, which holds no flag, when it has a resolving
    // member and every resolving member is locked, nested groups recursively. Any other id is not.
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

    // Walks up through any nesting to find the outermost group (recursively) containing the given
    // entity id - used so clicking any member of a group, however deeply nested, converges
    // selection on the top-level group.
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

    // The group that lists this id directly among its members, or null for a top-level entity.
    public Group? FindParentGroup(Guid memberId) =>
        _groups.Values.FirstOrDefault(group => group.MemberIds.Contains(memberId));

    // The content extent: every instance's committed bounds unioned with every edge end that
    // resolves, so a board holding only floating connectors still has something to frame. An end
    // that no longer resolves is skipped rather than read as the origin. Null for an empty board.
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

    // The Group counterpart to GetVisible - only top-level groups (not themselves nested inside
    // another Group) are candidates, since a nested group has no tab stop or rendering of its
    // own. A member-less group (every member since deleted) has no resolvable bounds and is
    // excluded rather than crashing.
    public IReadOnlyCollection<Group> GetVisibleGroups(Bounds viewport, double overscan = 0)
    {
        var expandedViewport = viewport.ExpandedBy(overscan);

        return _groups
            .Values.Where(group => FindContainingGroup(group.Id) is null)
            .Where(group => GetBounds(group) is { } bounds && expandedViewport.Intersects(bounds))
            .ToList();
    }

    // Layering is arithmetic-only field writes on ZIndex, never a renumbering pass over other
    // entities. NextZIndex is also what a newly-placed instance receives, so it always appears
    // above every existing one (a fixed baseline ZIndex was rejected as an alternative).
    public int NextZIndex() =>
        _components.Count == 0 ? 0 : _components.Values.Max(c => c.ZIndex) + 1;

    public int PreviousZIndex() =>
        _components.Count == 0 ? 0 : _components.Values.Min(c => c.ZIndex) - 1;

    // The next distinct ZIndex value strictly above/below the given one, skipping over any ties
    // at that same value - so bringing a component forward/backward past a whole cluster of tied
    // siblings takes one step, not one swap per sibling. Null when nothing exists on the far side
    // (already at that extreme).
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
