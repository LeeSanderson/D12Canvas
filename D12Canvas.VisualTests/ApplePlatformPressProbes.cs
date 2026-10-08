using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The browser reports an Apple platform, where Ctrl+click is the system secondary click.
public sealed class ApplePlatformPressProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    protected override string? BrowserInitScript =>
        """
            Object.defineProperty(Navigator.prototype, "platform", { get: () => "MacIntel" });
            Object.defineProperty(Navigator.prototype, "userAgentData", { get: () => undefined });
            """;

    [Fact]
    public async Task ACtrlPrimaryPress_IsASecondaryPressThatOpensTheMenu()
    {
        var rectangle = Page.Locator(".component-container[aria-label='Rectangle']").First;
        var centre = await CentreOfAsync(rectangle);
        await Page.Mouse.MoveAsync(centre.X, centre.Y);
        await Page.Keyboard.DownAsync("Control");
        await Page.Mouse.DownAsync();
        await Page.Mouse.UpAsync();
        await Page.Keyboard.UpAsync("Control");
        await SettleAsync();

        Assert.Equal(2, (await SinglePressAsync()).GetProperty("button").GetInt32());
        var release = Assert.Single(await CallsToAsync("OnPointerReleased"))[0];
        Assert.Equal(2, release.GetProperty("button").GetInt32());
        await Expect(Page.Locator(".d12-context-menu")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task TheMenusShortcutHints_AreDrawnInAppleSymbols()
    {
        await Page.Locator(".component-container[aria-label='Rectangle']")
            .First.ClickAsync(new() { Button = Microsoft.Playwright.MouseButton.Right });

        await Expect(Page.Locator(".d12-context-menu-hint"))
            .ToHaveTextAsync(["⌘X", "⌘C", "⌘V", "⌫", "⇧⌘]", "⌘]", "⌘[", "⇧⌘["]);
    }

    [Fact]
    public async Task ACtrlPrimaryDragOnEmptyCanvas_Pans()
    {
        var viewport = Page.Locator(".canvas-content");
        var before = await viewport.GetAttributeAsync("style");
        var start = await EmptyCanvasPointAsync();
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Keyboard.DownAsync("Control");
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X - 60, start.Y - 40, new() { Steps = 4 });
        await Page.Mouse.UpAsync();
        await Page.Keyboard.UpAsync("Control");
        await SettleAsync();

        Assert.NotEqual(before, await viewport.GetAttributeAsync("style"));
        await Expect(Page.Locator(".marquee-select")).ToHaveCountAsync(0);
    }
}
