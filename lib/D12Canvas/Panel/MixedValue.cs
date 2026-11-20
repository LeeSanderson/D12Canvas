namespace D12Canvas.Panel;

internal static class MixedValue
{
    public const string Label = "Mixed";

    public static bool AreEqual(EditorKind kind, object? first, object? second) =>
        kind == EditorKind.Color && first is string firstColour && second is string secondColour
            ? string.Equals(firstColour, secondColour, StringComparison.OrdinalIgnoreCase)
            : Equals(first, second);

    public static bool IsMixed(EditorKind kind, IReadOnlyList<object?> values) =>
        values.Skip(1).Any(value => !AreEqual(kind, values[0], value));
}
