namespace D12Canvas.Model;

public sealed class Board
{
    private readonly Dictionary<Guid, ComponentInstance> _components = new();
    private readonly Dictionary<Guid, Group> _groups = new();
    private readonly Dictionary<Guid, Edge> _edges = new();

    public IReadOnlyCollection<ComponentInstance> Components => _components.Values;
    public IReadOnlyCollection<Group> Groups => _groups.Values;
    public IReadOnlyCollection<Edge> Edges => _edges.Values;

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

    // Ticket 48/49/55: an endpoint's live board-space point. A PortEndpoint/CustomPortEndpoint
    // resolves from its referenced instance's current Bounds rather than stored - what lets an
    // attached edge track move/resize for free (ADR 0005); a FloatingEndpoint resolves to its own
    // fixed point, tracking nothing. Null when a referenced instance or custom port no longer exists.
    public (double X, double Y)? ResolveEndpoint(IEdgeEndpoint endpoint) =>
        endpoint switch
        {
            PortEndpoint port => ResolvePort(port),
            CustomPortEndpoint custom => ResolveCustomPort(custom),
            FloatingEndpoint floating => (floating.X, floating.Y),
            _ => null,
        };

    private (double X, double Y)? ResolvePort(PortEndpoint port)
    {
        var instance = GetComponent(port.ComponentId);
        if (instance is null)
        {
            return null;
        }

        var (fractionX, fractionY) = StandardPorts.FractionOf(port.PortId);
        return instance.Bounds.PointAtFraction(fractionX, fractionY);
    }

    private (double X, double Y)? ResolveCustomPort(CustomPortEndpoint endpoint)
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
                return instance.Bounds.PointAtFraction(port.FractionX, port.FractionY);
            }
        }

        return null;
    }

    // Ticket 48/55: geometric proximity hit-test for a connector-drag drop point - ADR 0005 settled
    // on discrete named ports (rejecting nearest-point-on-perimeter), so a drop only resolves to a
    // port when within tolerance of one of an instance's actual port points, standard or custom.
    // Returns the closest match across every instance's ports, or null when nothing is within
    // tolerance.
    public IEdgeEndpoint? FindPortNear((double X, double Y) point, double tolerance)
    {
        IEdgeEndpoint? closest = null;
        var closestDistance = double.MaxValue;

        foreach (var instance in _components.Values)
        {
            foreach (var (endpoint, fractionX, fractionY) in AllPorts(instance))
            {
                var (portX, portY) = instance.Bounds.PointAtFraction(fractionX, fractionY);
                var distance = DistanceFrom(portX, portY, point);

                if (distance <= tolerance && distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = endpoint;
                }
            }
        }

        return closest;
    }

    // Ticket 55: every port an instance exposes, standard and custom alike, each already paired
    // with the IEdgeEndpoint it resolves to - lets FindPortNear hit-test both kinds with a single
    // loop body instead of repeating it once per kind.
    private static IEnumerable<(
        IEdgeEndpoint Endpoint,
        double FractionX,
        double FractionY
    )> AllPorts(ComponentInstance instance)
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

    private static double DistanceFrom(double x, double y, (double X, double Y) point) =>
        Math.Sqrt(Math.Pow(x - point.X, 2) + Math.Pow(y - point.Y, 2));

    // Ticket 49/55: does any edge already anchor to this exact port (standard or custom)? Used to
    // tell "start a new edge" apart from "reposition this edge's existing endpoint" (see
    // DiagramCanvas.StartPortDrag). Multiple edges sharing the same port pick whichever is found
    // first - an acceptable ambiguity this ticket doesn't need to resolve.
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

    // Ticket 53: resolves an edge label's live ComponentInstance by its own id - used by
    // DiagramCanvas.CommitPropsChange to find the right object to mutate when a label's inline
    // text edit commits. A label has no Board.Components entry of its own (ADR 0005 embeds it
    // directly on its owning Edge), so it needs this separate lookup rather than GetComponent.
    public ComponentInstance? FindEdgeLabel(Guid instanceId) =>
        _edges.Values.Select(edge => edge.Label).FirstOrDefault(label => label?.Id == instanceId);

    // Ticket 44: a Group's bounds are never stored - only ever resolved on demand from its
    // members, which may themselves be component instances or nested groups. A member id that no
    // longer resolves to anything (deleted out from under the group) is skipped rather than
    // failing the whole computation.
    public Bounds? GetBounds(Group group) =>
        Bounds.Union(
            group.MemberIds.Select(MemberBounds).Where(b => b.HasValue).Select(b => b!.Value)
        );

    private Bounds? MemberBounds(Guid memberId) =>
        GetComponent(memberId)?.Bounds
        ?? (GetGroup(memberId) is { } nested ? GetBounds(nested) : null);

    // Ticket 44: walks up through any nesting to find the outermost group (recursively)
    // containing the given entity id - used so clicking any member of a group, however deeply
    // nested, converges selection on the top-level group (ADR 0006).
    public Group? FindContainingGroup(Guid memberId)
    {
        var parent = _groups.Values.FirstOrDefault(g => g.MemberIds.Contains(memberId));
        if (parent is null)
        {
            return null;
        }

        while (true)
        {
            var grandparent = _groups.Values.FirstOrDefault(g => g.MemberIds.Contains(parent.Id));
            if (grandparent is null)
            {
                return parent;
            }

            parent = grandparent;
        }
    }

    public IReadOnlyCollection<ComponentInstance> GetVisible(Bounds viewport, double overscan = 0)
    {
        var expandedViewport = viewport.ExpandedBy(overscan);

        return _components
            .Values.Where(instance => expandedViewport.Intersects(instance.Bounds))
            .ToList();
    }
}
