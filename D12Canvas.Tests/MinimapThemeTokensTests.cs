using Bunit;
using Xunit;

namespace D12Canvas.Tests;

// The minimap floats over the board, so it declares the raised values and color-scheme on its own
// root in the same four blocks Palette and PropertyPanel use, and everything it draws reads them.
public class MinimapThemeTokensTests : ComponentTestBase
{
    public MinimapThemeTokensTests()
    {
        SetupDiagramCanvasJsModule();
    }

    private string Css() => StyleBlockText(Render<Minimap>());

    [Fact]
    public void TokenDefaultsAreDeclaredOnTheMinimapOwnRootNotGlobalRoot()
    {
        var css = Css();

        var rootRule = ExtractEveryBlock(css, ".d12-minimap {");
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, rootRule);
        }

        Assert.DoesNotContain(":root", css);
    }

    [Fact]
    public void TheRootTakesThePaletteRaisedValues()
    {
        var rootRule = ExtractEveryBlock(Css(), ".d12-minimap {");

        Assert.Contains("--d12-surface: #fff", rootRule);
        Assert.Contains("--d12-border: #ccc", rootRule);
        Assert.Contains("--d12-muted-text: #666", rootRule);
    }

    [Fact]
    public void DarkColorSchemeMediaQueryRedeclaresEveryToken()
    {
        var darkMediaBlock = ExtractBlock(Css(), "@media (prefers-color-scheme: dark)");

        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, darkMediaBlock);
        }

        Assert.Contains("--d12-surface: #2a2a2a", darkMediaBlock);
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void DataThemeOverrideAppliesToTheMinimapItselfAndAnyAncestor(string theme)
    {
        var css = Css();

        var overrideBlock = ExtractBlock(css, $"[data-d12-theme={theme}] .d12-minimap {{");
        Assert.Contains($".d12-minimap[data-d12-theme={theme}]", css);
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, overrideBlock);
        }
    }

    [Fact]
    public void EveryRaisedBlockDeclaresItsColorScheme()
    {
        var css = Css();

        Assert.Contains("color-scheme: light", ExtractEveryBlock(css, ".d12-minimap {"));
        Assert.Contains(
            "color-scheme: dark",
            ExtractBlock(css, "@media (prefers-color-scheme: dark)")
        );
        Assert.Contains(
            "color-scheme: light",
            ExtractBlock(css, "[data-d12-theme=light] .d12-minimap {")
        );
        Assert.Contains(
            "color-scheme: dark",
            ExtractBlock(css, "[data-d12-theme=dark] .d12-minimap {")
        );
    }

    [Theory]
    [InlineData(".d12-minimap-box {")]
    [InlineData(".d12-minimap-viewport {")]
    public void WhatTheMinimapDrawsReadsTokensExclusively(string marker)
    {
        var block = ExtractBlock(Css(), marker);

        Assert.Contains("var(--d12-", block);
        Assert.DoesNotContain("#", block);
        Assert.DoesNotContain("rgba(", block);
    }
}
