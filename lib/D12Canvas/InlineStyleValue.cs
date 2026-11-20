namespace D12Canvas;

internal static class InlineStyleValue
{
    private static readonly char[] Breakouts =
    [
        ';',
        '{',
        '}',
        '<',
        '>',
        '"',
        '\'',
        '\\',
        '\r',
        '\n',
    ];

    private static readonly string[] Fetches = ["url(", "image-set(", "expression(", "/*"];

    public static string? Safe(string? value) =>
        value is null
        || value.IndexOfAny(Breakouts) >= 0
        || Fetches.Any(fetch => value.Contains(fetch, StringComparison.OrdinalIgnoreCase))
            ? null
            : value;

    public static string Declaration(string property, string? value) =>
        Safe(value) is { } safe ? $"{property}: {safe};" : "";

    public static string Join(params string[] declarations) =>
        string.Join(" ", declarations.Where(declaration => declaration.Length > 0));
}
