namespace D12Canvas;

// A binding as the keydown table matches it: Primary is Ctrl, or Cmd on an Apple platform, since
// every chord accepts either. AppleKey names the key where a Mac keyboard labels it differently.
internal sealed record Chord(
    string Key,
    bool Primary = false,
    bool Shift = false,
    bool Alt = false,
    string? AppleKey = null
);

// Apple platforms draw a chord as symbols in the fixed order ⌥⇧⌘ with no separator; everywhere
// else it is words joined by '+'. Substituting a modifier name into one string gives ⌘+Shift+Z,
// which is wrong on both.
internal static class ShortcutHint
{
    public static string Render(Chord chord, bool applePlatform) =>
        applePlatform ? AppleHint(chord) : WordHint(chord);

    private static string AppleHint(Chord chord) =>
        (chord.Alt ? "⌥" : "")
        + (chord.Shift ? "⇧" : "")
        + (chord.Primary ? "⌘" : "")
        + (chord.AppleKey ?? chord.Key);

    private static string WordHint(Chord chord)
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

        parts.Add(chord.Key);
        return string.Join("+", parts);
    }
}
