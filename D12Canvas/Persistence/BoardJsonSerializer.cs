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
            board.Components.Select(ToEnvelope).ToList()
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
            board.AddComponent(FromEnvelope(componentEnvelope));
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
            var entity = DescribeEntity(componentElement, index);
            index++;

            try
            {
                var componentEnvelope =
                    componentElement.Deserialize<ComponentInstanceEnvelope>(Options)
                    ?? throw new JsonException("The component entry is empty.");

                board.AddComponent(FromEnvelope(componentEnvelope));
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

        return new PartialBoardDeserializeResult(board, warnings);
    }

    private static void EnsureSupportedSchemaVersion(int schemaVersion)
    {
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new UnsupportedSchemaVersionException(CurrentSchemaVersion, schemaVersion);
        }
    }

    private static string DescribeEntity(JsonElement componentElement, int index)
    {
        if (
            componentElement.ValueKind == JsonValueKind.Object
            && componentElement.TryGetProperty(
                nameof(ComponentInstanceEnvelope.Id),
                out var idProperty
            )
            && idProperty.ValueKind == JsonValueKind.String
        )
        {
            return idProperty.GetString()!;
        }

        return $"{nameof(BoardEnvelope.Components)}[{index}]";
    }

    private static ComponentInstanceEnvelope ToEnvelope(ComponentInstance instance) =>
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

    private ComponentInstance FromEnvelope(ComponentInstanceEnvelope envelope)
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
