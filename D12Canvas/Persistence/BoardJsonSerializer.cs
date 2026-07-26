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

        if (envelope.SchemaVersion != CurrentSchemaVersion)
        {
            throw new UnsupportedSchemaVersionException(
                CurrentSchemaVersion,
                envelope.SchemaVersion
            );
        }

        var board = new Board();

        foreach (var componentEnvelope in envelope.Components)
        {
            board.AddComponent(FromEnvelope(componentEnvelope));
        }

        return board;
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
