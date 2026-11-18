using Bunit;

namespace D12Canvas.Tests;

public class D12IconTests : BunitContext
{
    private static readonly Glyph PaintedGlyph = new(
        new GlyphPart("M2 2h4"),
        new GlyphPart("M3 3h1v1h-1z", GlyphPaint.Fill),
        new GlyphPart("M4 4h2", GlyphPaint.AccentStroke),
        new GlyphPart("M5 5h1v1h-1z", GlyphPaint.AccentFill)
    );

    [Fact]
    public void AnIconWithoutALabelIsHiddenFromAssistiveTechnology()
    {
        var icon = Render<D12Icon>(parameters => parameters.Add(p => p.Glyph, Glyphs.Lock));

        var svg = icon.Find("svg");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Equal("false", svg.GetAttribute("focusable"));
        Assert.Null(svg.GetAttribute("role"));
        Assert.Null(svg.GetAttribute("aria-label"));
        Assert.Empty(icon.FindAll("title"));
    }

    [Fact]
    public void AnIconWithALabelIsAnImageWithThatNameAndNoTitle()
    {
        var icon = Render<D12Icon>(parameters =>
            parameters.Add(p => p.Glyph, Glyphs.Lock).Add(p => p.Label, "Locked")
        );

        var svg = icon.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("Locked", svg.GetAttribute("aria-label"));
        Assert.Null(svg.GetAttribute("aria-hidden"));
        Assert.Empty(icon.FindAll("title"));
    }

    [Fact]
    public void TheSvgDrawsOnThe16UnitGridInTheTextColourWithRoundCapsAndJoins()
    {
        var icon = Render<D12Icon>(parameters => parameters.Add(p => p.Glyph, Glyphs.Lock));

        var svg = icon.Find("svg");
        Assert.Equal("0 0 16 16", svg.GetAttribute("viewBox"));
        Assert.Equal("none", svg.GetAttribute("fill"));
        Assert.Equal("currentColor", svg.GetAttribute("stroke"));
        Assert.Equal("round", svg.GetAttribute("stroke-linecap"));
        Assert.Equal("round", svg.GetAttribute("stroke-linejoin"));
        Assert.Null(svg.GetAttribute("width"));
        Assert.Null(svg.GetAttribute("height"));
    }

    [Fact]
    public void EachPartIsOnePathAndOnlyAccentPartsCarryTheAccentClass()
    {
        var icon = Render<D12Icon>(parameters => parameters.Add(p => p.Glyph, PaintedGlyph));

        var paths = icon.FindAll("svg path");
        Assert.Equal(
            PaintedGlyph.Parts.Select(part => part.Data),
            paths.Select(path => path.GetAttribute("d"))
        );
        Assert.Equal(
            [false, false, true, true],
            paths.Select(path => path.ClassList.Contains("d12-icon-accent"))
        );
    }

    [Fact]
    public void TheClassParameterAndExtraAttributesReachTheSvg()
    {
        var icon = Render<D12Icon>(parameters =>
            parameters
                .Add(p => p.Glyph, Glyphs.Lock)
                .Add(p => p.Class, "host-icon")
                .AddUnmatched("data-testid", "lock-icon")
        );

        var svg = icon.Find("svg");
        Assert.True(svg.ClassList.Contains("d12-icon"));
        Assert.True(svg.ClassList.Contains("host-icon"));
        Assert.Equal("lock-icon", svg.GetAttribute("data-testid"));
    }

    [Fact]
    public void SizeStrokeAndAccentComeFromCustomPropertiesWithDefaults()
    {
        var icon = Render<D12Icon>(parameters => parameters.Add(p => p.Glyph, Glyphs.Lock));

        var css = icon.Find("style").InnerHtml;
        Assert.Contains("var(--d12-icon-size, 16px)", css);
        Assert.Contains("var(--d12-icon-stroke, 1.5)", css);
        Assert.Contains("var(--d12-icon-accent, currentColor)", css);
    }
}
