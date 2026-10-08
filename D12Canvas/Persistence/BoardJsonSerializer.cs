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
            board.Edges.Select(ToEdgeEnvelope).ToList(),
            ReferencedAssetEnvelopes(board)
        );

        return JsonSerializer.Serialize(envelope, Options);
    }

    // Only the assets some entity still refers to reach the file; an abandoned one stays in
    // memory until the board is reloaded. Null rather than empty so the property is omitted.
    private IReadOnlyList<AssetEnvelope>? ReferencedAssetEnvelopes(Board board)
    {
        var assets = ReferencedAssets
            .IdsReferencedBy(board.InstancesIncludingEdgeLabels(), _registry)
            .Distinct(StringComparer.Ordinal)
            .Select(board.GetAsset)
            .OfType<Asset>()
            .OrderBy(asset => asset.Id, StringComparer.Ordinal)
            .Select(asset => new AssetEnvelope(asset.Id, asset.MimeType, asset.Data))
            .ToList();

        return assets.Count == 0 ? null : assets;
    }

    public Board Deserialize(string json)
    {
        var envelope =
            JsonSerializer.Deserialize<BoardEnvelope>(json, Options)
            ?? throw new JsonException("The board envelope is empty.");

        EnsureSupportedSchemaVersion(envelope.SchemaVersion);

        var board = new Board();

        foreach (var assetEnvelope in envelope.Assets ?? [])
        {
            board.AddAsset(FromAssetEnvelope(assetEnvelope));
        }

        foreach (var componentEnvelope in envelope.Components)
        {
            board.AddComponent(FromComponentEnvelope(componentEnvelope));
        }

        foreach (var groupEnvelope in envelope.Groups ?? [])
        {
            board.AddGroup(FromGroupEnvelope(groupEnvelope));
        }

        PlanGroupRepair(board).ApplyTo(board);

        foreach (var edgeEnvelope in envelope.Edges ?? [])
        {
            board.AddEdge(FromEdgeEnvelope(edgeEnvelope));
        }

        return board;
    }

    private static GroupRepairPlan PlanGroupRepair(Board board) =>
        GroupRepair.Plan(board.Groups, id => board.GetComponent(id) is not null);

    public PartialBoardDeserializeResult DeserializePartial(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var schemaVersion = root.GetProperty(nameof(BoardEnvelope.SchemaVersion)).GetInt32();
        EnsureSupportedSchemaVersion(schemaVersion);

        var board = new Board();
        var warnings = new List<BoardDeserializeWarning>();

        if (root.TryGetProperty(nameof(BoardEnvelope.Assets), out var assetsElement))
        {
            ParseEntries<AssetEnvelope>(
                assetsElement,
                nameof(AssetEnvelope.Id),
                nameof(BoardEnvelope.Assets),
                warnings,
                (_, assetEnvelope) => board.AddAsset(FromAssetEnvelope(assetEnvelope))
            );
        }

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

        WarnAboutMissingAssets(board, warnings);

        return new PartialBoardDeserializeResult(board, warnings);
    }

    // A reference to an asset the file does not carry degrades rather than failing: the reference
    // is left in place, so the built-in Image lands on its "Image unavailable" state, and the
    // partial path says which instance points at what. The strict path tolerates it silently.
    private void WarnAboutMissingAssets(Board board, List<BoardDeserializeWarning> warnings)
    {
        foreach (var instance in board.InstancesIncludingEdgeLabels())
        {
            foreach (var assetId in ReferencedAssets.IdsReferencedBy([instance], _registry))
            {
                if (board.GetAsset(assetId) is null)
                {
                    warnings.Add(
                        new BoardDeserializeWarning(
                            instance.Id.ToString(),
                            $"References missing asset '{assetId}'."
                        )
                    );
                }
            }
        }
    }

    private static Asset FromAssetEnvelope(AssetEnvelope envelope) =>
        new(envelope.Id, envelope.MimeType, envelope.Data);

    // An edge referencing a missing instance is tolerated, not fatal - Board.ResolveEndpoint
    // already tolerates a dangling PortEndpoint componentId at read time (Model/Board.cs), so a
    // warning is recorded but the edge still loads, mirroring how a group's missing member is
    // handled just above. Edges never reference Groups, so no group-membership check is needed
    // here.
    private void DeserializeEdgesPartial(
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
            if (endpoint.ComponentId is { } componentId && board.GetComponent(componentId) is null)
            {
                yield return componentId;
            }
        }
    }

    // A group's problems never fail the load either. Every parsed group is added first, so a
    // nested reference to a group declared later in the array resolves, and the membership repair
    // then runs over the whole set at once: a member id that resolves to nothing is dropped, a
    // group left with no members is removed and a group left with one is dissolved into its
    // parent, each with its own warning. The strict path makes the same repair silently.
    private static void DeserializeGroupsPartial(
        JsonElement groupsElement,
        Board board,
        List<BoardDeserializeWarning> warnings
    )
    {
        var entityNames = new Dictionary<Guid, string>();

        ParseEntries<GroupEnvelope>(
            groupsElement,
            nameof(GroupEnvelope.Id),
            nameof(BoardEnvelope.Groups),
            warnings,
            (entity, groupEnvelope) =>
            {
                board.AddGroup(FromGroupEnvelope(groupEnvelope));
                entityNames[groupEnvelope.Id] = entity;
            }
        );

        var plan = PlanGroupRepair(board);

        string EntityName(Guid groupId) =>
            entityNames.TryGetValue(groupId, out var name) ? name : groupId.ToString();

        foreach (var (groupId, memberId) in plan.MissingMembers)
        {
            warnings.Add(
                new BoardDeserializeWarning(
                    EntityName(groupId),
                    $"References missing member '{memberId}'."
                )
            );
        }

        foreach (var group in plan.Emptied)
        {
            warnings.Add(
                new BoardDeserializeWarning(
                    EntityName(group.Id),
                    "No members remain; the group was removed."
                )
            );
        }

        foreach (var (group, survivorId) in plan.Dissolved)
        {
            warnings.Add(
                new BoardDeserializeWarning(
                    EntityName(group.Id),
                    $"One member remains; the group was dissolved and '{survivorId}' takes its place."
                )
            );
        }

        plan.ApplyTo(board);
    }

    private static void EnsureSupportedSchemaVersion(int schemaVersion)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new UnsupportedSchemaVersionException(CurrentSchemaVersion, schemaVersion);
        }
    }

    // The shared "iterate a JSON array, describe each entry for error reporting, deserialize it,
    // and turn any failure into a warning instead of aborting the load" shape - duplicated across
    // Components/Groups/Edges once Edges arrived as a third occurrence (the extraction was
    // deliberately deferred until then). `onEntry` does whatever each entity kind needs with a
    // successfully-parsed envelope (bind and add to the board, or - for Groups - stash it for a
    // second, cross-referencing pass); anything `onEntry` throws is still caught here and reported
    // the same way as a parse failure.
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
            edge.TargetArrow,
            edge.Label is null ? null : ToComponentEnvelope(edge.Label),
            edge.Color
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
            CustomPortEndpoint custom => new EdgeEndpointEnvelope(
                custom.ComponentId,
                null,
                null,
                null,
                custom.PortId
            ),
            FloatingEndpoint floating => new EdgeEndpointEnvelope(
                null,
                null,
                floating.X,
                floating.Y
            ),
            AutoPortEndpoint auto => new EdgeEndpointEnvelope(auto.ComponentId, null, null, null),
            _ => throw new NotSupportedException(
                $"Unsupported edge endpoint type '{endpoint.GetType()}'."
            ),
        };

    private Edge FromEdgeEnvelope(EdgeEnvelope envelope) =>
        new(
            FromEndpointEnvelope(envelope.Source),
            FromEndpointEnvelope(envelope.Target),
            envelope.Id,
            envelope.RoutingStyle,
            envelope.SourceArrow,
            envelope.TargetArrow,
            envelope.Label is null ? null : FromComponentEnvelope(envelope.Label),
            envelope.Color
        );

    private static IEdgeEndpoint FromEndpointEnvelope(EdgeEndpointEnvelope? envelope) =>
        envelope switch
        {
            { ComponentId: { } componentId, PortId: { } portId } => new PortEndpoint(
                componentId,
                portId
            ),
            { ComponentId: { } componentId, CustomPortId: { } customPortId } =>
                new CustomPortEndpoint(componentId, customPortId),
            { X: { } x, Y: { } y } => new FloatingEndpoint(x, y),
            { ComponentId: { } componentId } => new AutoPortEndpoint(componentId),
            _ => throw new JsonException("The edge endpoint is neither attached nor floating."),
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
            instance.ZIndex,
            instance
                .CustomPorts.Select(p => new PortDefEnvelope(p.Id, p.FractionX, p.FractionY))
                .ToList()
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
            envelope.Id,
            envelope.CustomPorts?.Select(p => new PortDef(p.Id, p.FractionX, p.FractionY)).ToList()
        );
    }
}
