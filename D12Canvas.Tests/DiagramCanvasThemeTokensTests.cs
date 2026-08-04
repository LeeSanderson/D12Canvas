using Bunit;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasThemeTokensTests : ComponentTestBase
{
    public DiagramCanvasThemeTokensTests()
    {
        SetupDiagramCanvasJsModule();
    }

    private static readonly string[] Tokens =
    [
        "--d12-surface",
        "--d12-border",
        "--d12-accent",
        "--d12-muted-text",
    ];

    private static string StyleBlockText(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find("style").InnerHtml;

    // Extracts the (possibly nested) `{ ... }` block immediately following the first occurrence of
    // `marker` - used both for a plain rule's own declaration block and for an @media block's whole
    // body (braces included), by counting nesting depth rather than matching the first `}`.
    private static string ExtractBlock(string css, string marker)
    {
        var markerIndex = css.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"Expected to find `{marker}` in the style block.");

        var openIndex = css.IndexOf('{', markerIndex);
        Assert.True(openIndex >= 0, $"Expected `{{` after `{marker}`.");

        var depth = 0;
        for (var i = openIndex; i < css.Length; i++)
        {
            if (css[i] == '{')
            {
                depth++;
            }
            else if (css[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return css.Substring(openIndex + 1, i - openIndex - 1);
                }
            }
        }

        throw new Xunit.Sdk.XunitException($"Unbalanced braces after `{marker}`.");
    }

    [Fact]
    public void TokenDefaultsAreDeclaredOnTheCanvasOwnRootNotGlobalRoot()
    {
        var canvas = Render<DiagramCanvas>();
        var css = StyleBlockText(canvas);

        var rootRule = ExtractBlock(css, ".diagram-container {");
        foreach (var token in Tokens)
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
        foreach (var token in Tokens)
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
        foreach (var token in Tokens)
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
}
