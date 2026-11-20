namespace D12Canvas;

internal sealed record Chord(
    string Key,
    bool Primary = false,
    bool Shift = false,
    bool Alt = false,
    string? AppleKey = null,
    string? Code = null
);

internal static class ShortcutHint
{
    public static string Render(
        Chord chord,
        bool applePlatform,
        IReadOnlyDictionary<string, string>? keyLabels = null
    )
    {
        var key = KeyName(chord, keyLabels);
        return applePlatform ? AppleHint(chord, chord.AppleKey ?? key) : WordHint(chord, key);
    }

    private static string KeyName(Chord chord, IReadOnlyDictionary<string, string>? keyLabels) =>
        chord.Code is { } code
        && keyLabels is not null
        && keyLabels.TryGetValue(code, out var label)
        && !string.IsNullOrWhiteSpace(label)
            ? label.ToUpperInvariant()
            : chord.Key;

    private static string AppleHint(Chord chord, string key) =>
        (chord.Alt ? "⌥" : "") + (chord.Shift ? "⇧" : "") + (chord.Primary ? "⌘" : "") + key;

    private static string WordHint(Chord chord, string key)
    {
        var parts = new List<string>();
        if (chord.Primary)
        {
            parts.Add("Ctrl");
        }

        if (chord.Alt)
        {
            parts.Add("Alt");
        }

        if (chord.Shift)
        {
            parts.Add("Shift");
        }

        parts.Add(key);
        return string.Join("+", parts);
    }
}
