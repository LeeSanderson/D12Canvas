namespace D12Canvas.App.Preferences;

// index.html reads Theme before Blazor starts, so it repeats that key as a literal.
public static class PreferenceKeys
{
    public const string Theme = "d12canvas.theme";
    public const string Autosave = "d12canvas.autosave";
}
