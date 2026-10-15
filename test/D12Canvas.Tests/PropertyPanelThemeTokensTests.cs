using Bunit;
using Xunit;

namespace D12Canvas.Tests;

// The panel floats over the board, so it declares the raised values on its own root, in the same
// four blocks Palette and the context menu use, and sets color-scheme in each so the parts of its
// native inputs that CSS cannot reach (the select's popup, the checkbox tick, the colour swatch
// frame) are drawn in the right scheme too.
public class PropertyPanelThemeTokensTests : ComponentTestBase
{
    [Fact]
    public void TokenDefaultsAreDeclaredOnThePanelOwnRootNotGlobalRoot()
    {
        var panel = Render<PropertyPanel>();
        var css = StyleBlockText(panel);

        var rootRule = ExtractEveryBlock(css, ".d12-property-panel {");
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, rootRule);
        }

        Assert.DoesNotContain(":root", css);
    }

    [Fact]
    public void DarkColorSchemeMediaQueryRedeclaresEveryToken()
    {
        var panel = Render<PropertyPanel>();
        var css = StyleBlockText(panel);

        var darkMediaBlock = ExtractBlock(css, "@media (prefers-color-scheme: dark)");
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, darkMediaBlock);
        }
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void DataThemeOverrideAppliesToThePanelItselfAndAnyAncestor(string theme)
    {
        var panel = Render<PropertyPanel>();
        var css = StyleBlockText(panel);

        var overrideBlock = ExtractBlock(css, $"[data-d12-theme={theme}] .d12-property-panel {{");
        Assert.Contains($".d12-property-panel[data-d12-theme={theme}]", css);
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, overrideBlock);
        }
    }

    [Fact]
    public void EveryRaisedBlockDeclaresItsColorScheme()
    {
        var panel = Render<PropertyPanel>();
        var css = StyleBlockText(panel);

        Assert.Contains("color-scheme: light", ExtractEveryBlock(css, ".d12-property-panel {"));
        Assert.Contains(
            "color-scheme: dark",
            ExtractBlock(css, "@media (prefers-color-scheme: dark)")
        );
        Assert.Contains(
            "color-scheme: light",
            ExtractBlock(css, "[data-d12-theme=light] .d12-property-panel {")
        );
        Assert.Contains(
            "color-scheme: dark",
            ExtractBlock(css, "[data-d12-theme=dark] .d12-property-panel {")
        );
    }

    [Fact]
    public void RootDeclaresATokenDrivenTextColorInputsInherit()
    {
        var panel = Render<PropertyPanel>();
        var css = StyleBlockText(panel);

        Assert.Contains("color: var(--d12-text)", ExtractEveryBlock(css, ".d12-property-panel {"));
        Assert.Contains("color: inherit", ExtractBlock(css, ".d12-property-panel-input {"));
    }

    [Fact]
    public void InputReadsTokensExclusively()
    {
        var panel = Render<PropertyPanel>();
        var css = StyleBlockText(panel);

        var inputRule = ExtractBlock(css, ".d12-property-panel-input {");
        Assert.Contains("background: var(--d12-surface)", inputRule);
        Assert.Contains("border: 1px solid var(--d12-border)", inputRule);
        Assert.DoesNotContain("#", inputRule);
        Assert.DoesNotContain("rgba(", inputRule);
    }

    [Theory]
    [InlineData(".d12-property-panel-label {")]
    [InlineData(".d12-property-panel-empty {")]
    public void MutedTextReadsTheTokenExclusively(string marker)
    {
        var panel = Render<PropertyPanel>();
        var css = StyleBlockText(panel);

        var block = ExtractBlock(css, marker);
        Assert.Contains("color: var(--d12-muted-text)", block);
        Assert.DoesNotContain("#", block);
        Assert.DoesNotContain("rgba(", block);
    }
}
