using Microsoft.AspNetCore.Components;

namespace D12Canvas;

/// <summary>
/// Draws one <see cref="D12Canvas.Glyph"/> as a 16-unit SVG in the surrounding text colour. The
/// size follows <c>--d12-icon-size</c> (16px by default), the stroke width follows
/// <c>--d12-icon-stroke</c> (1.5 by default) and accent parts follow <c>--d12-icon-accent</c>
/// (the text colour by default), each settable on any ancestor.
/// </summary>
/// <remarks>
/// Give the accessible name to the control that holds the icon and leave the icon decorative,
/// which it is when <see cref="Label"/> is null. Set <see cref="Label"/> only for an icon that
/// stands alone and carries meaning; it then reads as an image with that name.
/// </remarks>
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
