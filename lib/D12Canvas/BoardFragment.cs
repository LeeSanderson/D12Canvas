using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;

namespace D12Canvas;

internal static class BoardFragment
{
    public static Board Of(
        Board board,
        IEnumerable<Guid> selectedIds,
        IEnumerable<Guid> selectedEdgeIds,
        IComponentRegistry registry,
        bool withoutLocked = false
    )
    {
        var instanceIds = new HashSet<Guid>();
        var groups = new Dictionary<Guid, Group>();
        foreach (var id in selectedIds)
        {
            Collect(board, id, instanceIds, groups);
        }

        if (withoutLocked)
        {
            instanceIds.RemoveWhere(id => board.GetComponent(id)!.Locked);
        }

        var fragment = new Board();
        foreach (var id in instanceIds)
        {
            fragment.AddComponent(CopyOf(board.GetComponent(id)!));
        }

        foreach (var group in groups.Values)
        {
            fragment.AddGroup(
                new Group(
                    group
                        .MemberIds.Where(id => instanceIds.Contains(id) || groups.ContainsKey(id))
                        .ToList(),
                    group.Id
                )
            );
        }

        if (withoutLocked)
        {
            GroupRepair
                .Plan(fragment.Groups.ToList(), id => fragment.GetComponent(id) is not null)
                .ApplyTo(fragment);
        }

        var selectedEdges = selectedEdgeIds.ToHashSet();
        foreach (var edge in board.Edges.Where(edge => !(withoutLocked && edge.Locked)))
        {
            var copy =
                selectedEdges.Contains(edge.Id) ? CarriedEdge(board, edge, instanceIds)
                : IsInterior(edge, instanceIds) ? CopyOf(edge, edge.Source, edge.Target)
                : null;
            if (copy is not null)
            {
                fragment.AddEdge(copy);
            }
        }

        foreach (
            var assetId in ReferencedAssets.IdsReferencedBy(
                fragment.InstancesIncludingEdgeLabels(),
                registry
            )
        )
        {
            if (board.GetAsset(assetId) is { } asset)
            {
                fragment.AddAsset(asset);
            }
        }

        return fragment;
    }

    private static void Collect(
        Board board,
        Guid id,
        HashSet<Guid> instanceIds,
        Dictionary<Guid, Group> groups
    )
    {
        if (board.GetGroup(id) is { } group)
        {
            if (groups.TryAdd(group.Id, group))
            {
                foreach (var memberId in group.MemberIds)
                {
                    Collect(board, memberId, instanceIds, groups);
                }
            }
        }
        else if (board.GetComponent(id) is not null)
        {
            instanceIds.Add(id);
        }
    }

    private static bool IsInterior(Edge edge, HashSet<Guid> instanceIds) =>
        edge.Source.ComponentId is { } sourceId
        && instanceIds.Contains(sourceId)
        && edge.Target.ComponentId is { } targetId
        && instanceIds.Contains(targetId);

    private static Edge? CarriedEdge(Board board, Edge edge, HashSet<Guid> instanceIds)
    {
        var source = CarriedEnd(board, edge, isSource: true, instanceIds);
        var target = CarriedEnd(board, edge, isSource: false, instanceIds);
        return source is null || target is null ? null : CopyOf(edge, source, target);
    }

    private static IEdgeEndpoint? CarriedEnd(
        Board board,
        Edge edge,
        bool isSource,
        HashSet<Guid> instanceIds
    )
    {
        var end = isSource ? edge.Source : edge.Target;
        if (end.ComponentId is not { } componentId || instanceIds.Contains(componentId))
        {
            return end;
        }

        return board.ResolveEnd(edge, isSource) is { } point
            ? new FloatingEndpoint(point.X, point.Y)
            : null;
    }

    private static ComponentInstance CopyOf(ComponentInstance instance) =>
        new(
            instance.ComponentTypeKey,
            instance.Props,
            instance.Bounds,
            instance.ZIndex,
            instance.Id,
            instance.CustomPorts,
            instance.Locked
        );

    private static Edge CopyOf(Edge edge, IEdgeEndpoint source, IEdgeEndpoint target) =>
        new(
            source,
            target,
            edge.Id,
            edge.RoutingStyle,
            edge.SourceArrow,
            edge.TargetArrow,
            edge.Label is null ? null : CopyOf(edge.Label),
            edge.Color,
            edge.Locked
        );

    public static Board WithFreshIds(Board fragment)
    {
        var instanceIds = fragment.Components.ToDictionary(instance => instance.Id, _ => NewId());
        var groupIds = fragment.Groups.ToDictionary(group => group.Id, _ => NewId());
        var portIds = new Dictionary<(Guid InstanceId, Guid PortId), Guid>();

        var result = new Board();
        foreach (var asset in fragment.Assets)
        {
            result.AddAsset(asset);
        }

        foreach (var instance in fragment.Components)
        {
            result.AddComponent(
                Renamed(instance, instanceIds[instance.Id], port => portIds[port] = NewId())
            );
        }

        foreach (var group in fragment.Groups)
        {
            result.AddGroup(
                new Group(
                    group
                        .MemberIds.Select(id =>
                            instanceIds.TryGetValue(id, out var instanceId) ? instanceId
                            : groupIds.TryGetValue(id, out var groupId) ? groupId
                            : (Guid?)null
                        )
                        .OfType<Guid>()
                        .ToList(),
                    groupIds[group.Id]
                )
            );
        }

        foreach (var edge in fragment.Edges)
        {
            var source = Renamed(edge.Source, instanceIds, portIds);
            var target = Renamed(edge.Target, instanceIds, portIds);
            if (source is null || target is null)
            {
                continue;
            }

            result.AddEdge(
                new Edge(
                    source,
                    target,
                    NewId(),
                    edge.RoutingStyle,
                    edge.SourceArrow,
                    edge.TargetArrow,
                    edge.Label is null ? null : Renamed(edge.Label, NewId(), _ => NewId()),
                    edge.Color,
                    edge.Locked
                )
            );
        }

        return result;
    }

    private static Guid NewId() => Guid.NewGuid();

    private static ComponentInstance Renamed(
        ComponentInstance instance,
        Guid id,
        Func<(Guid InstanceId, Guid PortId), Guid> newPortId
    ) =>
        new(
            instance.ComponentTypeKey,
            instance.Props,
            instance.Bounds,
            instance.ZIndex,
            id,
            instance
                .CustomPorts.Select(port => port with { Id = newPortId((instance.Id, port.Id)) })
                .ToList(),
            instance.Locked
        );

    private static IEdgeEndpoint? Renamed(
        IEdgeEndpoint end,
        Dictionary<Guid, Guid> instanceIds,
        Dictionary<(Guid InstanceId, Guid PortId), Guid> portIds
    )
    {
        if (end.ComponentId is { } componentId && !instanceIds.ContainsKey(componentId))
        {
            return null;
        }

        return end switch
        {
            PortEndpoint port => port with { ComponentId = instanceIds[port.ComponentId] },
            CustomPortEndpoint custom => new CustomPortEndpoint(
                instanceIds[custom.ComponentId],
                portIds.TryGetValue((custom.ComponentId, custom.PortId), out var portId)
                    ? portId
                    : custom.PortId
            ),
            AutoPortEndpoint auto => new AutoPortEndpoint(instanceIds[auto.ComponentId]),
            _ => end,
        };
    }

    public static IReadOnlyList<Edge> DropEdgesWithMissingEnds(Board fragment)
    {
        var dropped = fragment
            .Edges.Where(edge =>
                new[] { edge.Source, edge.Target }.Any(end =>
                    end.ComponentId is { } id && fragment.GetComponent(id) is null
                )
            )
            .ToList();
        foreach (var edge in dropped)
        {
            fragment.RemoveEdge(edge.Id);
        }

        return dropped;
    }

    public static Bounds? Extent(Board fragment) => fragment.ContentExtent();

    public static void Translate(Board fragment, double dx, double dy)
    {
        foreach (var instance in fragment.Components)
        {
            instance.Bounds = instance.Bounds with
            {
                X = instance.Bounds.X + dx,
                Y = instance.Bounds.Y + dy,
            };
        }

        foreach (var edge in fragment.Edges)
        {
            edge.Source = Translated(edge.Source, dx, dy);
            edge.Target = Translated(edge.Target, dx, dy);
        }
    }

    private static IEdgeEndpoint Translated(IEdgeEndpoint end, double dx, double dy) =>
        end is FloatingEndpoint floating
            ? new FloatingEndpoint(floating.X + dx, floating.Y + dy)
            : end;

    public static Placement PlaceOnto(Board board, Board fragment)
    {
        StackAbove(board, fragment);

        var rejectedAssetIds = new List<string>();
        foreach (var asset in fragment.Assets)
        {
            if (Asset.IdFor(asset.Data) == asset.Id)
            {
                board.AddAsset(asset);
            }
            else
            {
                rejectedAssetIds.Add(asset.Id);
            }
        }

        var commands = new List<ICommand>();
        commands.AddRange(
            fragment.Components.Select(instance => new AddEntityCommand(board, instance))
        );
        commands.AddRange(fragment.Groups.Select(group => new GroupCommand(board, group)));
        commands.AddRange(fragment.Edges.Select(edge => new AddEdgeCommand(board, edge)));

        var nested = fragment.Groups.SelectMany(group => group.MemberIds).ToHashSet();
        var topLevel = fragment
            .Components.Select(instance => instance.Id)
            .Concat(fragment.Groups.Select(group => group.Id))
            .Where(id => !nested.Contains(id))
            .ToList();

        return new Placement(
            new CompositeCommand(commands),
            topLevel,
            fragment.Edges.Select(edge => edge.Id).ToList(),
            rejectedAssetIds
        );
    }

    public static void StackAbove(Board board, Board fragment)
    {
        var baseZIndex = board.NextZIndex();
        var ranks = fragment
            .Components.Select(instance => instance.ZIndex)
            .Distinct()
            .Order()
            .Select((zIndex, rank) => (zIndex, rank))
            .ToDictionary(pair => pair.zIndex, pair => pair.rank);
        foreach (var instance in fragment.Components)
        {
            instance.ZIndex = baseZIndex + ranks[instance.ZIndex];
        }
    }

    public sealed record Placement(
        ICommand Command,
        IReadOnlyList<Guid> TopLevelIds,
        IReadOnlyList<Guid> EdgeIds,
        IReadOnlyList<string> RejectedAssetIds
    );
}
