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

        var rootRule = ExtractBlock(css, ".d12-palette {");
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

        var overrideBlock = ExtractBlock(css, $"[data-d12-theme=\"{theme}\"] .d12-palette {{");
        Assert.Contains($".d12-palette[data-d12-theme=\"{theme}\"]", css);
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, overrideBlock);
        }
    }

    [Fact]
    public void CategoryTitleReadsTokensExclusively()
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var title = ExtractBlock(css, ".d12-palette-category-title {");
        Assert.Contains("var(--d12-muted-text)", title);
        Assert.DoesNotContain("#", title);
        Assert.DoesNotContain("rgba(", title);
    }

    [Fact]
    public void EntryButtonHoverReadsTokensExclusively()
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var hover = ExtractBlock(css, ".d12-palette-entry-button:hover {");
        Assert.Contains("var(--d12-surface)", hover);
        Assert.Contains("var(--d12-border)", hover);
        Assert.DoesNotContain("#", hover);
        Assert.DoesNotContain("rgba(", hover);
    }

    // .d12-palette-entry-button sets color: inherit (overriding the button element's own UA
    // default) - without the root itself setting a token-driven color, entry names would fall
    // through to whatever ambient text color the host page happens to have, which stays
    // low-contrast dark text even once the palette's own background goes dark.
    [Fact]
    public void RootDeclaresATokenDrivenTextColorEntryNamesInherit()
    {
        var palette = Render<Palette>();
        var css = StyleBlockText(palette);

        var rootRule = ExtractBlock(css, ".d12-palette {");
        Assert.Contains("color: var(--d12-text)", rootRule);
    }
}
