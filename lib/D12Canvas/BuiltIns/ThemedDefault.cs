namespace D12Canvas.BuiltIns;

// A built-in paints a board token when its colour prop is null and the author's literal when it
// is not. The literal travels as an override custom property that the component's own rule
// prefers over the token, so the swap needs no knowledge of which theme is live.
internal static class ThemedDefault
{
    public static string Override(string token, string? authoredValue) =>
        InlineStyleValue.Safe(authoredValue) is { } value ? $"{token}: {value}; " : "";
}
