using Bunit;
using Xunit;

namespace D12Canvas.Tests;

public class ContextMenuThemeTokensTests : ComponentTestBase
{
    public ContextMenuThemeTokensTests()
    {
        JSInterop
            .SetupModule("./_content/D12Canvas/ContextMenu.razor.js")
            .SetupModule("registerMenu", _ => true)
            .SetupVoid("dispose", _ => true)
            .SetVoidResult();
    }

    private IRenderedComponent<ContextMenu> RenderMenu() =>
        Render<ContextMenu>(parameters =>
            parameters.Add(p => p.Context, new ContextMenuContext(ContextMenuSet.Object))
        );

    [Fact]
    public void HintReadsTheMutedTextToken()
    {
        var css = StyleBlockText(RenderMenu());

        var hint = ExtractBlock(css, ".d12-context-menu-hint {");
        Assert.Contains("color: var(--d12-muted-text)", hint);
    }

    [Fact]
    public void TokenDefaultsAreDeclaredOnTheMenuOwnRootNotGlobalRoot()
    {
        var menu = RenderMenu();
        var css = StyleBlockText(menu);

        var rootRule = ExtractEveryBlock(css, ".d12-context-menu {");
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, rootRule);
        }

        Assert.DoesNotContain(":root", css);
    }

    [Fact]
    public void DarkColorSchemeMediaQueryRedeclaresEveryToken()
    {
        var menu = RenderMenu();
        var css = StyleBlockText(menu);

        var darkMediaBlock = ExtractBlock(css, "@media (prefers-color-scheme: dark)");
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, darkMediaBlock);
        }
    }

    [Theory]
    [InlineData("light")]
    [InlineData("dark")]
    public void DataThemeOverrideAppliesToTheMenuItselfAndAnyAncestor(string theme)
    {
        var menu = RenderMenu();
        var css = StyleBlockText(menu);

        var overrideBlock = ExtractBlock(css, $"[data-d12-theme={theme}] .d12-context-menu {{");
        Assert.Contains($".d12-context-menu[data-d12-theme={theme}]", css);
        foreach (var token in ThemeTokens)
        {
            Assert.Contains(token, overrideBlock);
        }
    }

    [Fact]
    public void MenuItemHoverReadsTokensExclusively()
    {
        var menu = RenderMenu();
        var css = StyleBlockText(menu);

        var hover = ExtractBlock(css, ".d12-context-menu-item:hover {");
        Assert.Contains("var(--d12-surface)", hover);
        Assert.Contains("var(--d12-border)", hover);
        Assert.DoesNotContain("#", hover);
        Assert.DoesNotContain("rgba(", hover);
    }

    [Fact]
    public void SeparatorReadsTokensExclusively()
    {
        var menu = RenderMenu();
        var css = StyleBlockText(menu);

        var separator = ExtractBlock(css, ".d12-context-menu-separator {");
        Assert.Contains("var(--d12-border)", separator);
        Assert.DoesNotContain("#", separator);
        Assert.DoesNotContain("rgba(", separator);
    }

    // .d12-context-menu-item sets color: inherit (overriding the button element's own UA
    // default) - without the root itself setting a token-driven color, menu item labels would
    // fall through to whatever ambient text color the host page happens to have, which stays
    // low-contrast dark text even once the menu's own background goes dark.
    [Fact]
    public void RootDeclaresATokenDrivenTextColorMenuItemsInherit()
    {
        var menu = RenderMenu();
        var css = StyleBlockText(menu);

        var rootRule = ExtractEveryBlock(css, ".d12-context-menu {");
        Assert.Contains("color: var(--d12-text)", rootRule);
    }

    [Fact]
    public void DropShadowReadsItsOwnEscapeHatchTokenExclusively()
    {
        var menu = RenderMenu();
        var css = StyleBlockText(menu);

        var rootRule = ExtractEveryBlock(css, ".d12-context-menu {");
        Assert.Contains("--d12-shadow", rootRule);
        Assert.Contains("box-shadow: 0 2px 8px var(--d12-shadow)", rootRule);
    }

    [Theory]
    [InlineData(".d12-context-menu {", "rgba(0, 0, 0, 0.15)")]
    [InlineData("@media (prefers-color-scheme: dark)", "rgba(0, 0, 0, 0.5)")]
    [InlineData("[data-d12-theme=light] .d12-context-menu {", "rgba(0, 0, 0, 0.15)")]
    [InlineData("[data-d12-theme=dark] .d12-context-menu {", "rgba(0, 0, 0, 0.5)")]
    public void TheDropShadowDeepensOnDark(string marker, string shadow)
    {
        var menu = RenderMenu();
        var block = ExtractBlock(StyleBlockText(menu), marker);

        Assert.Contains($"--d12-shadow: {shadow};", block);
    }
}
