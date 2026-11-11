namespace D12Canvas.App.Components;

public sealed record ThemeChoice(string Value, string Label, string Icon)
{
    public const string PreferenceKey = "d12canvas.theme";

    public static readonly ThemeChoice Light = new("light", "Light", "sun-fill");
    public static readonly ThemeChoice Dark = new("dark", "Dark", "moon-stars-fill");
    public static readonly ThemeChoice System = new("system", "System", "circle-half");

    public static IReadOnlyList<ThemeChoice> All { get; } = [Light, Dark, System];

    public static ThemeChoice Parse(string? stored) =>
        All.FirstOrDefault(choice => choice.Value == stored) ?? System;
}
