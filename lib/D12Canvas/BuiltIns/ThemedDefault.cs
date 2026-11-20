namespace D12Canvas.BuiltIns;

internal static class ThemedDefault
{
    public static string Override(string token, string? authoredValue) =>
        InlineStyleValue.Safe(authoredValue) is { } value ? $"{token}: {value}; " : "";
}
