using System.Text.RegularExpressions;

namespace D12Canvas.Registration;

internal sealed partial class ColourPresetCatalog
{
    public static readonly IReadOnlyList<ColourPreset> Defaults =
    [
        new("Black", "#000000"),
        new("Grey", "#868E96"),
        new("White", "#FFFFFF"),
        new("Red", "#E03131"),
        new("Orange", "#F08C00"),
        new("Yellow", "#FFEB3B"),
        new("Green", "#2F9E44"),
        new("Blue", "#2F80ED"),
        new("Purple", "#7048E8"),
        new("Pink", "#D6336C"),
    ];

    private IReadOnlyList<ColourPreset> _presets = Defaults;

    public IReadOnlyList<ColourPreset> Presets
    {
        get => _presets;
        set => _presets = Validated(value);
    }

    private static IReadOnlyList<ColourPreset> Validated(IReadOnlyList<ColourPreset>? presets)
    {
        ArgumentNullException.ThrowIfNull(presets, nameof(D12CanvasOptions.ColourPresets));

        foreach (var preset in presets)
        {
            if (preset is null)
            {
                throw new ArgumentException(
                    "A colour preset must not be null.",
                    nameof(D12CanvasOptions.ColourPresets)
                );
            }

            if (string.IsNullOrWhiteSpace(preset.Name))
            {
                throw new ArgumentException(
                    $"The colour preset '{preset.Hex}' needs a Name.",
                    nameof(D12CanvasOptions.ColourPresets)
                );
            }

            if (preset.Hex is null || !HexPattern().IsMatch(preset.Hex))
            {
                throw new ArgumentException(
                    $"The colour preset '{preset.Name}' has Hex '{preset.Hex}', which is not #RRGGBB.",
                    nameof(D12CanvasOptions.ColourPresets)
                );
            }
        }

        return presets.ToArray();
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexPattern();
}
