using Microsoft.AspNetCore.Components;

namespace D12Canvas;

public partial class D12Icon
{
    [Parameter, EditorRequired]
    public Glyph Glyph { get; set; } = null!;

    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter]
    private D12IconStyles? StylesInScope { get; set; }

    private string SvgClass => Class is null ? "d12-icon" : $"d12-icon {Class}";

    private static bool IsFill(GlyphPaint paint) =>
        paint is GlyphPaint.Fill or GlyphPaint.AccentFill;

    private static string? PartClass(GlyphPaint paint) =>
        paint switch
        {
            GlyphPaint.Fill => "d12-icon-fill",
            GlyphPaint.AccentStroke => "d12-icon-accent",
            GlyphPaint.AccentFill => "d12-icon-accent d12-icon-fill",
            _ => null,
        };
}
