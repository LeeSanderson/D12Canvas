using System.Text.Json;
using D12Canvas.Model;
using D12Canvas.Registration;

namespace D12Canvas.Persistence;

public sealed class BoardJsonSerializer : IBoardSerializer
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly IComponentRegistry _registry;

    public BoardJsonSerializer(IComponentRegistry registry)
    {
        _registry = registry;
    }

    public string Serialize(Board board)
    {
        var envelope = new BoardEnvelope(
            CurrentSchemaVersion,
            board.Components.Select(ToComponentEnvelope).ToList(),
            board.Groups.Select(ToGroupEnvelope).ToList(),
            board.Edges.Select(ToEdgeEnvelope).ToList()
        );

        return JsonSerializer.Serialize(envelope, Options);
    }

    public Board Deserialize(string json)
    {
        var envelope =
            JsonSerializer.Deserialize<BoardEnvelope>(json, Options)
            ?? throw new JsonException("The board envelope is empty.");

        EnsureSupportedSchemaVersion(envelope.SchemaVersion);

        var board = new Board();

        foreach (var componentEnvelope in envelope.Components)
        {
            board.AddComponent(FromComponentEnvelope(componentEnvelope));
        }

        foreach (var groupEnvelope in envelope.Groups ?? [])
        {
            board.AddGroup(FromGroupEnvelope(groupEnvelope));
        }

        foreach (var edgeEnvelope in envelope.Edges ?? [])
        {
            board.AddEdge(FromEdgeEnvelope(edgeEnvelope));
        }

        return board;
    }

    public PartialBoardDeserializeResult DeserializePartial(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var schemaVersion = root.GetProperty(nameof(BoardEnvelope.SchemaVersion)).GetInt32();
        EnsureSupportedSchemaVersion(schemaVersion);

        var board = new Board();
        var warnings = new List<BoardDeserializeWarning>();

        ParseEntries<ComponentInstanceEnvelope>(
            root.GetProperty(nameof(BoardEnvelope.Components)),
            nameof(ComponentInstanceEnvelope.Id),
            nameof(BoardEnvelope.Components),
            warnings,
            (_, componentEnvelope) => board.AddComponent(FromComponentEnvelope(componentEnvelope))
        );

        if (root.TryGetProperty(nameof(BoardEnvelope.Groups), out var groupsElement))
        {
            DeserializeGroupsPartial(groupsElement, board, warnings);
        }

        if (root.TryGetProperty(nameof(BoardEnvelope.Edges), out var edgesElement))
        {
            DeserializeEdgesPartial(edgesElement, board, warnings);
        }

        return new PartialBoardDeserializeResult(board, warnings);
    }

    // Ticket 51: an edge referencing a missing instance is tolerated, not fatal - Board.ResolveEndpoint
    // already tolerates a dangling PortEndpoint componentId at read time (Model/Board.cs), so a
    // warning is recorded but the edge still loads, mirroring how a group's missing member is
    // handled just above. Edges never reference Groups (ticket 15), so no group-membership check
    // is needed here.
    private static void DeserializeEdgesPartial(
        JsonElement edgesElement,
        Board board,
        List<BoardDeserializeWarning> warnings
    )
    {
        ParseEntries<EdgeEnvelope>(
            edgesElement,
            nameof(EdgeEnvelope.Id),
            nameof(BoardEnvelope.Edges),
            warnings,
            (entity, edgeEnvelope) =>
            {
                var edge = FromEdgeEnvelope(edgeEnvelope);

                foreach (var missingComponentId in MissingComponentIds(edge, board))
                {
                    warnings.Add(
                        new BoardDeserializeWarning(
                            entity,
                            $"References missing instance '{missingComponentId}'."
                        )
                    );
                }

                board.AddEdge(edge);
            }
        );
    }

    private static IEnumerable<Guid> MissingComponentIds(Edge edge, Board board)
    {
        foreach (var endpoint in new[] { edge.Source, edge.Target })
        {
            if (endpoint is PortEndpoint port && board.GetComponent(port.ComponentId) is null)
            {
                yield return port.ComponentId;
            }
        }
    }

    // A group referencing a member (component or nested group) that doesn't exist is tolerated,
    // not fatal - Board.GetBounds already tolerates a dangling member id at read time (Model/Board.cs),
    // so a warning is recorded but the group still loads with whatever members do resolve.
    // Membership is checked against every successfully-parsed group in the same batch regardless of
    // array order, since nesting may reference a group declared later in the Groups array.
    private static void DeserializeGroupsPartial(
        JsonElement groupsElement,
        Board board,
        List<BoardDeserializeWarning> warnings
    )
    {
        var parsedGroups = new List<(string Entity, GroupEnvelope Envelope)>();

        ParseEntries<GroupEnvelope>(
            groupsElement,
            nameof(GroupEnvelope.Id),
            nameof(BoardEnvelope.Groups),
            warnings,
            (entity, groupEnvelope) => parsedGroups.Add((entity, groupEnvelope))
        );

        var knownIds = new HashSet<Guid>(board.Components.Select(c => c.Id));
        knownIds.UnionWith(parsedGroups.Select(g => g.Envelope.Id));

        foreach (var (entity, groupEnvelope) in parsedGroups)
        {
            foreach (var memberId in groupEnvelope.MemberIds.Where(id => !knownIds.Contains(id)))
            {
                warnings.Add(
                    new BoardDeserializeWarning(entity, $"References missing member '{memberId}'.")
                );
            }

            try
            {
                board.AddGroup(FromGroupEnvelope(groupEnvelope));
            }
            catch (Exception ex)
            {
                // Mirrors the Components loop above: a duplicate Id (e.g. two Groups entries
                // sharing an Id) must never abort the rest of the load either.
                warnings.Add(
                    new BoardDeserializeWarning(entity, $"Malformed entity: {ex.Message}")
                );
            }
        }
    }

    private static void EnsureSupportedSchemaVersion(int schemaVersion)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new UnsupportedSchemaVersionException(CurrentSchemaVersion, schemaVersion);
        }
    }

    // Ticket 51: the shared "iterate a JSON array, describe each entry for error reporting,
    // deserialize it, and turn any failure into a warning instead of aborting the load" shape -
    // duplicated across Components/Groups/Edges once Edges arrived as a third occurrence (ticket
    // 46 deliberately deferred this extraction until then). `onEntry` does whatever each entity
    // kind needs with a successfully-parsed envelope (bind and add to the board, or - for Groups -
    // stash it for a second, cross-referencing pass); anything `onEntry` throws is still caught
    // here and reported the same way as a parse failure.
    private static void ParseEntries<TEnvelope>(
        JsonElement arrayElement,
        string idPropertyName,
        string arrayPropertyName,
        List<BoardDeserializeWarning> warnings,
        Action<string, TEnvelope> onEntry
    )
    {
        var index = 0;
        foreach (var element in arrayElement.EnumerateArray())
        {
            var entity = DescribeEntity(element, idPropertyName, arrayPropertyName, index);
            index++;

            try
            {
                var envelope =
                    element.Deserialize<TEnvelope>(Options)
                    ?? throw new JsonException("The entry is empty.");

                onEntry(entity, envelope);
            }
            catch (UnknownComponentKeyException ex)
            {
                warnings.Add(
                    new BoardDeserializeWarning(entity, $"Unknown component type '{ex.Key}'.")
                );
            }
            catch (Exception ex)
            {
                // Deliberately broad: any failure to parse or bind one entity (or a duplicate Id
                // added later by onEntry) must never abort the rest of the load.
                warnings.Add(
                    new BoardDeserializeWarning(entity, $"Malformed entity: {ex.Message}")
                );
            }
        }
    }

    private static string DescribeEntity(
        JsonElement element,
        string idPropertyName,
        string arrayPropertyName,
        int index
    )
    {
        if (
            element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(idPropertyName, out var idProperty)
            && idProperty.ValueKind == JsonValueKind.String
        )
        {
            return idProperty.GetString()!;
        }

        return $"{arrayPropertyName}[{index}]";
    }

    private static GroupEnvelope ToGroupEnvelope(Group group) => new(group.Id, group.MemberIds);

    private static Group FromGroupEnvelope(GroupEnvelope envelope) =>
        new(envelope.MemberIds, envelope.Id);

    private static EdgeEnvelope ToEdgeEnvelope(Edge edge) =>
        new(
            edge.Id,
            ToEndpointEnvelope(edge.Source),
            ToEndpointEnvelope(edge.Target),
            edge.RoutingStyle,
            edge.SourceArrow,
            edge.TargetArrow
        );

    private static EdgeEndpointEnvelope ToEndpointEnvelope(IEdgeEndpoint endpoint) =>
        endpoint switch
        {
            PortEndpoint port => new EdgeEndpointEnvelope(
                port.ComponentId,
                port.PortId,
                null,
                null
            ),
            FloatingEndpoint floating => new EdgeEndpointEnvelope(
                null,
                null,
                floating.X,
                floating.Y
            ),
            _ => throw new NotSupportedException(
                $"Unsupported edge endpoint type '{endpoint.GetType()}'."
            ),
        };

    private static Edge FromEdgeEnvelope(EdgeEnvelope envelope) =>
        new(
            FromEndpointEnvelope(envelope.Source),
            FromEndpointEnvelope(envelope.Target),
            envelope.Id,
            envelope.RoutingStyle,
            envelope.SourceArrow,
            envelope.TargetArrow
        );

    private static IEdgeEndpoint FromEndpointEnvelope(EdgeEndpointEnvelope? envelope) =>
        envelope switch
        {
            { ComponentId: { } componentId, PortId: { } portId } => new PortEndpoint(
                componentId,
                portId
            ),
            { X: { } x, Y: { } y } => new FloatingEndpoint(x, y),
            _ => throw new JsonException(
                "The edge endpoint is neither port-attached nor floating."
            ),
        };

    private static ComponentInstanceEnvelope ToComponentEnvelope(ComponentInstance instance) =>
        new(
            instance.Id,
            instance.ComponentTypeKey,
            instance.Props,
            new BoundsEnvelope(
                instance.Bounds.X,
                instance.Bounds.Y,
                instance.Bounds.Width,
                instance.Bounds.Height
            ),
            instance.ZIndex
        );

    private ComponentInstance FromComponentEnvelope(ComponentInstanceEnvelope envelope)
    {
        var registration = _registry.Resolve(envelope.ComponentTypeKey);
        var propsElement = (JsonElement)envelope.Props!;
        var props =
            propsElement.Deserialize(registration.PropsType, Options)
            ?? throw new JsonException($"Component '{envelope.Id}' has null props.");

        return new ComponentInstance(
            envelope.ComponentTypeKey,
            props,
            new Bounds(
                envelope.Bounds.X,
                envelope.Bounds.Y,
                envelope.Bounds.Width,
                envelope.Bounds.Height
            ),
            envelope.ZIndex,
            envelope.Id
        );
    }
}
