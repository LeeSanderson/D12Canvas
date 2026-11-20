using Bunit;
using Xunit;

namespace D12Canvas.Tests;

public class PaletteThemeTokensTests : ComponentTestBase
{
    [Fact]
    public void TokenDefaultsAreDeclaredOnThePaletteOwnRootNotGlobalRoot()
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var rootRule = ExtractEveryBlock(css, ".d12-palette {");
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, rootRule);
        }

        Assert.DoesNotContain(":root", css);
    }

    [Fact]
    public void DarkColorSchemeMediaQueryRedeclaresEveryToken()
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var darkMediaBlock = ExtractBlock(css, "@media (prefers-color-scheme: dark)");
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, darkMediaBlock);
        }
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void DataThemeOverrideAppliesToThePaletteItselfAndAnyAncestor(string theme)
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var overrideBlock = ExtractBlock(css, $"[data-d12-theme={theme}] .d12-palette {{");
        Assert.Contains($".d12-palette[data-d12-theme={theme}]", css);
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, overrideBlock);
        }
    }

    [Fact]
    public void TooltipReadsTokensExclusively()
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var tooltip = ExtractBlock(css, ".d12-tooltip {");
        Assert.Contains("var(--d12-text)", tooltip);
        Assert.Contains("var(--d12-surface)", tooltip);
        Assert.DoesNotContain("#", tooltip);
        Assert.DoesNotContain("rgba(", tooltip);
    }

    [Fact]
    public void FlyoutReadsTokensExclusively()
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var flyout = ExtractBlock(css, ".d12-flyout {");
        Assert.Contains("var(--d12-surface)", flyout);
        Assert.Contains("var(--d12-border)", flyout);
        Assert.Contains("var(--d12-shadow)", flyout);
        Assert.DoesNotContain("#", flyout);
        Assert.DoesNotContain("rgba(", flyout);
    }

    [Fact]
    public void ButtonHoverReadsTokensExclusively()
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var hover = ExtractBlock(css, ".d12-palette-button:hover {");
        Assert.Contains("var(--d12-surface)", hover);
        Assert.Contains("var(--d12-border)", hover);
        Assert.DoesNotContain("#", hover);
        Assert.DoesNotContain("rgba(", hover);
    }

    [Fact]
    public void RootDeclaresATokenDrivenTextColorTheGlyphsInherit()
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var rootRule = ExtractEveryBlock(css, ".d12-palette {");
        Assert.Contains("color: var(--d12-text)", rootRule);
    }
}
