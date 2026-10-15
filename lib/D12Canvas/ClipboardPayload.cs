using System.Text.Json;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;

namespace D12Canvas;

// The clipboard carries a board envelope as plain text, recognised by its shape rather than by a
// marker, so the text of a saved board file pastes as well as a copy does. Anything else that has
// text in it becomes a text shape.
internal static class ClipboardPayload
{
    public const string TextComponentKey = "text";

    public sealed record Read(Board Fragment, IReadOnlyList<BoardDeserializeWarning> Warnings);

    public static bool IsBoard(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty(nameof(BoardEnvelope.SchemaVersion), out var version)
                && version.ValueKind == JsonValueKind.Number
                && version.TryGetInt32(out var schemaVersion)
                && schemaVersion == BoardJsonSerializer.CurrentSchemaVersion
                && root.TryGetProperty(nameof(BoardEnvelope.Components), out var components)
                && components.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // A recognised board goes through the same tolerant load a saved file can, so one entity that
    // cannot be bound costs that entity and a warning. Every id in the result is new.
    public static Read? From(string text, IBoardSerializer serializer, IComponentRegistry registry)
    {
        if (IsBoard(text))
        {
            PartialBoardDeserializeResult loaded;
            try
            {
                loaded = serializer.DeserializePartial(text);
            }
            catch (Exception exception)
                when (exception is JsonException or InvalidOperationException or FormatException)
            {
                return null;
            }

            BoardFragment.DropEdgesWithMissingEnds(loaded.Board);
            return new Read(BoardFragment.WithFreshIds(loaded.Board), loaded.Warnings);
        }

        return TextFragment(text, registry) is { } fragment ? new Read(fragment, []) : null;
    }

    private static Board? TextFragment(string text, IComponentRegistry registry)
    {
        if (
            string.IsNullOrWhiteSpace(text)
            || registry.All.FirstOrDefault(candidate => candidate.Key == TextComponentKey)
                is not { DefaultProps: TextProps defaults } registration
        )
        {
            return null;
        }

        var size = registration.DefaultSize ?? new ComponentSize(200, 40);
        var fragment = new Board();
        fragment.AddComponent(
            new ComponentInstance(
                registration.Key,
                defaults with
                {
                    Text = text,
                },
                new Bounds(0, 0, size.Width, size.Height)
            )
        );
        return fragment;
    }
}
