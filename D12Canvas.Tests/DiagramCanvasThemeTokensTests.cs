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
}
