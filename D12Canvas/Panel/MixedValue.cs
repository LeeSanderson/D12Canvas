namespace D12Canvas.Panel;

// Decides whether a property row's targets disagree, and whether a target already holds a
// committed value and can be skipped. Null is a value of its own, since a
// null colour follows the theme and a literal does not; colours compare without regard to case,
// since the native colour input reports lowercase and the built-in defaults are uppercase.
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
