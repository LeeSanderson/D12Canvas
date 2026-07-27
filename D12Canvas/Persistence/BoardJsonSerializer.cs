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
            board.Groups.Select(ToGroupEnvelope).ToList()
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

        var index = 0;
        foreach (
            var componentElement in root.GetProperty(nameof(BoardEnvelope.Components))
                .EnumerateArray()
        )
        {
            var entity = DescribeEntity(
                componentElement,
                nameof(ComponentInstanceEnvelope.Id),
                nameof(BoardEnvelope.Components),
                index
            );
            index++;

            try
            {
                var componentEnvelope =
                    componentElement.Deserialize<ComponentInstanceEnvelope>(Options)
                    ?? throw new JsonException("The component entry is empty.");

                board.AddComponent(FromComponentEnvelope(componentEnvelope));
            }
            catch (UnknownComponentKeyException ex)
            {
                warnings.Add(
                    new BoardDeserializeWarning(entity, $"Unknown component type '{ex.Key}'.")
                );
            }
            catch (Exception ex)
            {
                // Deliberately broad: any failure to parse or bind one entity must never
                // abort the rest of the load, so it's reported as a warning instead.
                warnings.Add(
                    new BoardDeserializeWarning(entity, $"Malformed entity: {ex.Message}")
                );
            }
        }

        if (root.TryGetProperty(nameof(BoardEnvelope.Groups), out var groupsElement))
        {
            DeserializeGroupsPartial(groupsElement, board, warnings);
        }

        return new PartialBoardDeserializeResult(board, warnings);
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

        var index = 0;
        foreach (var groupElement in groupsElement.EnumerateArray())
        {
            var entity = DescribeEntity(
                groupElement,
                nameof(GroupEnvelope.Id),
                nameof(BoardEnvelope.Groups),
                index
            );
            index++;

            try
            {
                var groupEnvelope =
                    groupElement.Deserialize<GroupEnvelope>(Options)
                    ?? throw new JsonException("The group entry is empty.");

                parsedGroups.Add((entity, groupEnvelope));
            }
            catch (Exception ex)
            {
                warnings.Add(
                    new BoardDeserializeWarning(entity, $"Malformed entity: {ex.Message}")
                );
            }
        }

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
