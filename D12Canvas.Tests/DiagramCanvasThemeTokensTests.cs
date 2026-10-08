using Bunit;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasThemeTokensTests : ComponentTestBase
{
    public DiagramCanvasThemeTokensTests()
    {
        SetupDiagramCanvasJsModule();
    }

    [Fact]
    public void TokenDefaultsAreDeclaredOnTheCanvasOwnRootNotGlobalRoot()
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);

        var rootRule = ExtractBlock(css, ".diagram-container {");
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, rootRule);
        }

        Assert.DoesNotContain(":root", css);
    }

    [Fact]
    public void DarkColorSchemeMediaQueryRedeclaresEveryToken()
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);

        var darkMediaBlock = ExtractBlock(css, "@media (prefers-color-scheme: dark)");
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, darkMediaBlock);
        }
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void DataThemeOverrideAppliesToTheCanvasItselfAndAnyAncestor(string theme)
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);

        var overrideBlock = ExtractBlock(
            css,
            $"[data-d12-theme=\"{theme}\"] .diagram-container {{"
        );
        Assert.Contains($".diagram-container[data-d12-theme=\"{theme}\"]", css);
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, overrideBlock);
        }
    }

    [Fact]
    public void GridReadsTokensExclusively()
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);

        var backdrop = ExtractBlock(css, ".grid-backdrop {");
        Assert.Contains("var(--d12-surface)", backdrop);
        Assert.DoesNotContain("#", backdrop);
        Assert.DoesNotContain("rgba(", backdrop);

        var layer = ExtractBlock(css, ".grid-layer {");
        Assert.Contains("var(--d12-surface)", layer);
        Assert.Contains("var(--d12-border)", layer);
        Assert.DoesNotContain("#", layer);
        Assert.DoesNotContain("rgba(", layer);
    }

    [Fact]
    public void MarqueeReadsTokensExclusively()
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);

        var marquee = ExtractBlock(css, ".marquee-select {");
        Assert.Contains("var(--d12-accent)", marquee);
        Assert.DoesNotContain("#", marquee);
        Assert.DoesNotContain("rgba(", marquee);
    }

    [Fact]
    public void LodPlaceholderReadsTokensExclusively()
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);

        var placeholder = ExtractBlock(css, ".lod-placeholder {");
        Assert.Contains("var(--d12-surface)", placeholder);
        Assert.Contains("var(--d12-border)", placeholder);
        Assert.Contains("var(--d12-muted-text)", placeholder);
        Assert.DoesNotContain("#", placeholder);
        Assert.DoesNotContain("rgba(", placeholder);
    }

    // The board tokens are what a built-in paints when its colour prop is null. They are declared
    // beside the chrome tokens in every block, with light values equal to the literals they
    // replaced, and stay separate from --d12-text so a host retuning its chrome label colour
    // does not silently recolour every text instance on every board.
    [Theory]
    [InlineData(".diagram-container {")]
    [InlineData("@media (prefers-color-scheme: dark)")]
    [InlineData("[data-d12-theme=\"light\"] .diagram-container {")]
    [InlineData("[data-d12-theme=\"dark\"] .diagram-container {")]
    public void EveryBlockDeclaresTheBoardTokens(string marker)
    {
        var canvas = Render<DiagramCanvas>();
        var block = ExtractBlock(StyleBlockText(canvas), marker);

        Assert.Contains("--d12-board-text:", block);
        Assert.Contains("--d12-board-fill:", block);
        Assert.Contains("--d12-board-stroke:", block);
    }

    [Fact]
    public void LightBoardTokensHaveTheirDeclaredValues()
    {
        var canvas = Render<DiagramCanvas>();
        var rootRule = ExtractBlock(StyleBlockText(canvas), ".diagram-container {");

        Assert.Contains("--d12-board-text: #000000", rootRule);
        Assert.Contains("--d12-board-fill: #FFFFFF", rootRule);
        Assert.Contains("--d12-board-stroke: #333333", rootRule);
    }

    // The connector drag-preview's green is a deliberate departure from the shared accent (which
    // already means "selected") - an escape hatch for an element that genuinely needs to diverge,
    // routed through its own custom property rather than a bare literal so it still counts as
    // "reading a token."
    [Fact]
    public void ConnectorDragPreviewReadsItsOwnEscapeHatchTokenExclusively()
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);

        var rootRule = ExtractBlock(css, ".diagram-container {");
        Assert.Contains("--d12-connector-preview", rootRule);

        var preview = ExtractBlock(css, ".connector-drag-preview {");
        Assert.Contains("var(--d12-connector-preview)", preview);
        Assert.DoesNotContain("#", preview);
        Assert.DoesNotContain("rgba(", preview);
    }

    [Theory]
    [InlineData(".diagram-container {")]
    [InlineData("@media (prefers-color-scheme: dark)")]
    [InlineData("[data-d12-theme=\"light\"] .diagram-container {")]
    [InlineData("[data-d12-theme=\"dark\"] .diagram-container {")]
    public void EveryBlockDeclaresTheAlignmentGuideToken(string marker)
    {
        var canvas = Render<DiagramCanvas>();
        var block = ExtractBlock(StyleBlockText(canvas), marker);

        Assert.Contains("--d12-alignment-guide:", block);
    }

    [Fact]
    public void GuidesReadTheirOwnTokenAtHalfIntensityAndNeverTheAccent()
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);
        var guide = ExtractBlock(css, ".spacing-guide {");

        Assert.Contains("stroke: var(--d12-alignment-guide)", guide);
        Assert.Contains("stroke-opacity: 0.5", guide);
        Assert.DoesNotContain("--d12-accent", guide);
        Assert.DoesNotContain("#", guide);

        var light = ExtractBlock(css, "[data-d12-theme=\"light\"] .diagram-container {");
        var dark = ExtractBlock(css, "[data-d12-theme=\"dark\"] .diagram-container {");
        Assert.NotEqual(
            TokenValue(light, "--d12-alignment-guide"),
            TokenValue(light, "--d12-accent")
        );
        Assert.NotEqual(
            TokenValue(dark, "--d12-alignment-guide"),
            TokenValue(dark, "--d12-accent")
        );
    }

    [Theory]
    [InlineData(".diagram-container {", "#4a4a4a")]
    [InlineData("@media (prefers-color-scheme: dark)", "#a0a0a0")]
    [InlineData("[data-d12-theme=\"light\"] .diagram-container {", "#4a4a4a")]
    [InlineData("[data-d12-theme=\"dark\"] .diagram-container {", "#a0a0a0")]
    public void EveryBlockDeclaresTheEdgeToken(string marker, string expected)
    {
        var canvas = Render<DiagramCanvas>();
        var block = ExtractBlock(StyleBlockText(canvas), marker);

        Assert.Equal(expected, TokenValue(block, "--d12-edge"));
    }

    [Theory]
    [InlineData("[data-d12-theme=\"light\"] .diagram-container {")]
    [InlineData("[data-d12-theme=\"dark\"] .diagram-container {")]
    public void TheEdgeTokenKeepsAtLeastThreeToOneContrastAgainstTheSurface(string marker)
    {
        var canvas = Render<DiagramCanvas>();
        var block = ExtractBlock(StyleBlockText(canvas), marker);

        var ratio = ContrastRatio(
            TokenValue(block, "--d12-edge"),
            TokenValue(block, "--d12-surface")
        );

        Assert.True(ratio >= 3, $"contrast {ratio:F2} is below 3:1");
    }

    [Fact]
    public void AnEdgeLinePaintsItsOverrideFallingBackToTheEdgeTokenAndNoLiteral()
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);
        var line = ExtractBlock(css, ".edge-line {");

        Assert.Contains("stroke: var(--d12-edge-override, var(--d12-edge))", line);
        Assert.DoesNotContain("#", line);
        Assert.DoesNotContain(".edge-line.selected", css);
        Assert.DoesNotContain(".edge-arrowhead", css);
    }

    [Fact]
    public void TheSelectionHaloIsATranslucentAccentStroke()
    {
        var canvas = Render<DiagramCanvas>();
        var halo = ExtractBlock(StyleBlockText(canvas), ".edge-halo {");

        Assert.Contains("stroke: var(--d12-accent)", halo);
        Assert.Contains("stroke-opacity:", halo);
        Assert.DoesNotContain("#", halo);
    }

    private static double ContrastRatio(string first, string second)
    {
        var a = RelativeLuminance(first);
        var b = RelativeLuminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static double RelativeLuminance(string hex)
    {
        double Channel(int offset)
        {
            var value = Convert.ToInt32(hex.Substring(offset, 2), 16) / 255.0;
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
    }

    private static string TokenValue(string block, string token)
    {
        var start = block.IndexOf(token + ":", StringComparison.Ordinal) + token.Length + 1;
        return block[start..block.IndexOf(';', start)].Trim();
    }
}
