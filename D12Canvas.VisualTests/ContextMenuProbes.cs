using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The menu dies on the next press, and what that press does depends on where it lands: inside the
// canvas container it only closes the menu, outside it the host's own markup still acts. Keys
// pressed in the menu stay in the menu. None of this is visible to bUnit, which never dispatches a
// real event through the capture phase or up to the window listener.
public sealed class ContextMenuProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SourceId = "a0000000-0000-0000-0000-000000000001";
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";

    private static readonly LocatorClickOptions RightClick = new() { Button = MouseButton.Right };

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator Menu => Page.Locator(".d12-context-menu");

    private async Task OpenMenuOnSourceAsync()
    {
        await Instance(SourceId).ClickAsync(RightClick);
        await Expect(Menu).ToBeVisibleAsync();
        await Expect(Instance(SourceId)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Menu).ToBeFocusedAsync();
        await SettleAsync();
        await ClearCallsAsync();
    }

    [Fact]
    public async Task TheMenusShortcutHints_AreDrawnInWordsOffApplePlatforms()
    {
        await OpenMenuOnSourceAsync();

        await Expect(Menu.Locator(".d12-context-menu-hint"))
            .ToHaveTextAsync(
                [
                    "Ctrl+X",
                    "Ctrl+C",
                    "Ctrl+V",
                    "Ctrl+D",
                    "Delete",
                    "Ctrl+Shift+]",
                    "Ctrl+]",
                    "Ctrl+[",
                    "Ctrl+Shift+[",
                    "Ctrl+Shift+L",
                ]
            );
    }

    [Fact]
    public async Task APressOnAHostButtonOutsideTheContainer_ClosesTheMenuAndActivatesTheButton()
    {
        await Page.EvaluateAsync(
            """
            () => {
                const button = document.createElement("button");
                button.className = "probe-host-button";
                button.textContent = "Host action";
                button.dataset.clicks = "0";
                button.addEventListener("click", () => {
                    button.dataset.clicks = String(Number(button.dataset.clicks) + 1);
                });
                document.body.append(button);
            }
            """
        );
        await OpenMenuOnSourceAsync();

        await Page.Locator(".probe-host-button").ClickAsync();

        await Expect(Menu).ToHaveCountAsync(0);
        await Expect(Page.Locator(".probe-host-button")).ToHaveAttributeAsync("data-clicks", "1");
    }

    [Fact]
    public async Task APressInsideTheContainer_OnlyClosesTheMenu()
    {
        await OpenMenuOnSourceAsync();

        await Instance(TargetId).ClickAsync();
        await SettleAsync();

        await Expect(Menu).ToHaveCountAsync(0);
        Assert.Equal(0, await PointerEntryPointCallCountAsync());
        await Expect(Instance(SourceId)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Instance(TargetId)).Not.ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task ASecondaryPressInsideTheContainer_ClosesTheMenuWithNoBrowserMenu()
    {
        await OpenMenuOnSourceAsync();
        await Page.EvaluateAsync(
            """
            () => {
                window.__d12ContextMenuPrevented = [];
                window.addEventListener("contextmenu", (event) => {
                    setTimeout(() => window.__d12ContextMenuPrevented.push(event.defaultPrevented));
                }, true);
            }
            """
        );

        await Instance(TargetId).ClickAsync(RightClick);
        await SettleAsync();

        await Expect(Menu).ToHaveCountAsync(0);
        Assert.Equal(0, await PointerEntryPointCallCountAsync());
        var prevented = await Page.EvaluateAsync<bool[]>("() => window.__d12ContextMenuPrevented");
        Assert.Equal([true], prevented);
    }

    [Fact]
    public async Task EscapeInTheMenu_ClosesItAndKeepsTheSelection()
    {
        await OpenMenuOnSourceAsync();

        await Page.Keyboard.PressAsync("Escape");
        await SettleAsync();

        await Expect(Menu).ToHaveCountAsync(0);
        await Expect(Instance(SourceId)).ToHaveAttributeAsync("aria-selected", "true");
        Assert.Empty(await CallsToAsync("OnEscapePressed"));
    }

    [Fact]
    public async Task ArrowsRoveThroughTheRows_AndNeverNudgeTheSelection()
    {
        await OpenMenuOnSourceAsync();
        var styleBefore = await Instance(SourceId).GetAttributeAsync("style");
        var rows = Menu.Locator(".d12-context-menu-item");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(rows.First).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(rows.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowUp");
        await SettleAsync();

        await Expect(rows.Last).ToBeFocusedAsync();
        Assert.Empty(await CallsToAsync("OnArrowKeyPressed"));
        Assert.Equal(styleBefore, await Instance(SourceId).GetAttributeAsync("style"));
    }

    [Fact]
    public async Task AMenuOpenedNearTheBottomRightCorner_OpensUpAndLeftWithNoRowClipped()
    {
        var corner = await PagePointOnCanvasAsync(690, 490);

        await Page.Mouse.ClickAsync(corner.X, corner.Y, new() { Button = MouseButton.Right });
        await Expect(Menu).ToBeVisibleAsync();

        var container = await Page.Locator(".diagram-container").BoundingBoxAsync();
        var menu = await Menu.BoundingBoxAsync();
        Assert.NotNull(container);
        Assert.NotNull(menu);
        Assert.True(menu!.X >= container!.X, $"menu left {menu.X} < container {container.X}");
        Assert.True(menu.Y >= container.Y, $"menu top {menu.Y} < container {container.Y}");
        var boxes =
            $"menu {menu.X},{menu.Y} {menu.Width}x{menu.Height}; container "
            + $"{container.X},{container.Y} {container.Width}x{container.Height}; "
            + $"anchor {corner.X},{corner.Y}";
        // The press point reaches the canvas rounded to a whole pixel, so the anchor may sit a
        // pixel away from where the mouse was sent.
        Assert.True(menu.X + menu.Width <= corner.X + 2, $"the menu should open left: {boxes}");
        Assert.True(menu.Y + menu.Height <= corner.Y + 2, $"the menu should open up: {boxes}");
        foreach (var row in await Menu.Locator(".d12-context-menu-item").AllAsync())
        {
            var box = await row.BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box!.Y + box.Height <= container.Y + container.Height);
        }
    }

    [Fact]
    public async Task TheAlignStripIsOneRowToTheVerticalArrows_AndTheSideArrowsRoveAcrossIt()
    {
        await Instance(SourceId).ClickAsync();
        await Instance(TargetId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(Page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);
        await Page.Keyboard.PressAsync("Shift+F10");
        await Expect(Menu.Locator(".d12-context-menu-item").First).ToBeFocusedAsync();
        await SettleAsync();
        await ClearCallsAsync();
        var focusedGlyph = Menu.Locator(".d12-context-menu-glyph:focus");

        for (var presses = 0; presses < 20 && await focusedGlyph.CountAsync() == 0; presses++)
        {
            await Page.Keyboard.PressAsync("ArrowDown");
        }

        await Expect(focusedGlyph).ToHaveAttributeAsync("title", "Align left");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(focusedGlyph).ToHaveAttributeAsync("title", "Align bottom");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(focusedGlyph).ToHaveAttributeAsync("title", "Align left");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Menu.Locator(".d12-context-menu-item:focus .d12-context-menu-label"))
            .ToHaveTextAsync("Bring to Front");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(focusedGlyph).ToHaveAttributeAsync("title", "Align left");

        await Page.Keyboard.PressAsync("Enter");

        await Expect(Menu).ToHaveCountAsync(0);
        Assert.Equal(await LeftOfAsync(SourceId), await LeftOfAsync(TargetId));
        Assert.Empty(await CallsToAsync("OnArrowKeyPressed"));
    }

    private async Task<string> LeftOfAsync(string id)
    {
        var style = await Instance(id).GetAttributeAsync("style");
        return Regex.Match(style!, @"left: (-?[\d.]+)px").Groups[1].Value;
    }
}
