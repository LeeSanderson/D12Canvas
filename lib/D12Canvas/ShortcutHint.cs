namespace D12Canvas;

// A binding as the keydown table matches it: Primary is Ctrl, or Cmd on an Apple platform, since
// every chord accepts either. Key is the US name of the key, AppleKey the name where a Mac keyboard
// labels it differently, and Code the physical key the table matches when the hint should follow
// the user's layout. A chord the browser's own clipboard events carry has no Code, since the
// operating system already maps those through the layout.
internal sealed record Chord(
    string Key,
    bool Primary = false,
    bool Shift = false,
    bool Alt = false,
    string? AppleKey = null,
    string? Code = null
);

// Apple platforms draw a chord as symbols in the fixed order ⌥⇧⌘ with no separator; everywhere
// else it is words joined by '+'. Substituting a modifier name into one string gives ⌘+Shift+Z,
// which is wrong on both.
internal static class ShortcutHint
{
    // keyLabels maps a physical key's code to what the user's layout prints on it, when the
    // browser can say; without it a hint names the US key.
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
