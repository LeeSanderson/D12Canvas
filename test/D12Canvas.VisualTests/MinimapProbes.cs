using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The real pointer and keyboard paths over the minimap demo, whose minimap floats over the
// canvas's bottom-right corner. The minimap's own listener reports to the same four entry points
// as the canvas's, so the recorded calls cover both.
public sealed class MinimapProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string NorthWestId = "a1000000-0000-0000-0000-000000000001";
    private const string NorthEastId = "a1000000-0000-0000-0000-000000000002";

    protected override string ProbePagePath => "/minimap-demo";

    protected override int ProbePageInstanceCount => 4;

    private ILocator Minimap => Page.Locator(".d12-minimap");

    private ILocator Rect => Page.Locator(".d12-minimap-viewport");

    private ILocator Content => Page.Locator(".canvas-content");

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        return box!;
    }

    private Task<string?> ContentStyleAsync() => Content.GetAttributeAsync("style");

    private Task<bool> FocusIsInsideTheMinimapAsync() =>
        Page.EvaluateAsync<bool>(
            "() => document.querySelector('.d12-minimap').contains(document.activeElement)"
        );

    private static void AssertNear(double expected, double actual) =>
        Assert.InRange(actual, expected - 3, expected + 3);

    [Fact]
    public async Task AClickOnTheMinimapFliesTheCanvasThereAndFocusesTheCanvas()
    {
        var target = await CentreOfAsync(Page.Locator(".d12-minimap-box").Nth(1));

        await Page.Mouse.ClickAsync(target.X, target.Y);
        await SettleAsync();

        Assert.Single(await CallsToAsync("OnPointerPressed"));
        Assert.Single(await CallsToAsync("OnPointerReleased"));
        Assert.Empty(await CallsToAsync("OnPointerMoved"));
        var container = await BoxOfAsync(Page.Locator(".diagram-container"));
        var shape = await BoxOfAsync(Instance(NorthEastId));
        AssertNear(container.X + container.Width / 2, shape.X + shape.Width / 2);
        AssertNear(container.Y + container.Height / 2, shape.Y + shape.Height / 2);
        Assert.True(
            await Page.EvaluateAsync<bool>(
                "() => document.activeElement?.classList.contains('diagram-canvas') === true"
            )
        );
    }

    [Fact]
    public async Task ADragPansLiveAndAReleaseOutsideTheMinimapEndsIt()
    {
        var minimap = await BoxOfAsync(Minimap);
        var start = (X: minimap.X + minimap.Width / 2, Y: minimap.Y + minimap.Height / 2);
        var opened = await ContentStyleAsync();

        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X - 30, start.Y - 20, new() { Steps = 4 });
        await SettleAsync();

        var midDrag = await ContentStyleAsync();
        Assert.NotEqual(opened, midDrag);
        Assert.DoesNotContain("transition", midDrag);
        Assert.NotEmpty(await CallsToAsync("OnPointerMoved"));

        await Page.Mouse.MoveAsync(minimap.X - 20, minimap.Y + 10, new() { Steps = 6 });
        await Page.Mouse.UpAsync();
        await SettleAsync();
        Assert.Single(await CallsToAsync("OnPointerReleased"));

        await ExpectNothingRespondsToAButtonlessMoveAsync();
        var released = await ContentStyleAsync();
        await Page.Mouse.MoveAsync(start.X, start.Y, new() { Steps = 6 });
        await Page.Mouse.MoveAsync(start.X - 40, start.Y + 20, new() { Steps = 6 });
        await SettleAsync();
        Assert.Equal(released, await ContentStyleAsync());
    }

    [Fact]
    public async Task EscapeDuringAMinimapDragStopsThePanUntilTheButtonComesUp()
    {
        var minimap = await BoxOfAsync(Minimap);
        var start = (X: minimap.X + minimap.Width / 2, Y: minimap.Y + minimap.Height / 2);
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X - 30, start.Y - 20, new() { Steps = 4 });
        await SettleAsync();

        await Page.Keyboard.PressAsync("Escape");
        await SettleAsync();
        var cancelled = await ContentStyleAsync();
        await Page.Mouse.MoveAsync(start.X + 30, start.Y + 20, new() { Steps = 4 });
        await SettleAsync();

        Assert.Equal(cancelled, await ContentStyleAsync());
        await Page.Mouse.UpAsync();
        await SettleAsync();
        Assert.Single(await CallsToAsync("OnPointerReleased"));
    }

    [Fact]
    public async Task AViewportKeyUnderAStillMinimapPressPromotesItSoTheNextSmallMovePans()
    {
        var minimap = await BoxOfAsync(Minimap);
        var start = (X: minimap.X + minimap.Width / 2, Y: minimap.Y + minimap.Height / 2);
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await SettleAsync();

        await Page.Keyboard.PressAsync("PageDown");
        await SettleAsync();
        await ClearCallsAsync();
        await Page.Mouse.MoveAsync(start.X + 2, start.Y);
        await SettleAsync();

        Assert.NotEmpty(await CallsToAsync("OnPointerMoved"));
        await Page.Mouse.UpAsync();
        await SettleAsync();
    }

    [Fact]
    public async Task ASecondaryPressOnTheMinimapOpensNoBrowserMenuAndPansNothing()
    {
        var before = await ContentStyleAsync();
        var minimap = await BoxOfAsync(Minimap);
        var prevented = Page.EvaluateAsync<bool>(
            """
            () => new Promise((resolve) => window.addEventListener(
                "contextmenu",
                (event) => setTimeout(() => resolve(event.defaultPrevented)),
                { once: true }
            ))
            """
        );

        await Page.Mouse.ClickAsync(
            minimap.X + 20,
            minimap.Y + 20,
            new() { Button = MouseButton.Right }
        );
        await SettleAsync();

        Assert.True(await prevented);
        Assert.Empty(await CallsToAsync("OnPointerPressed"));
        Assert.Equal(before, await ContentStyleAsync());
    }

    [Fact]
    public async Task TheMinimapIsHiddenAndTabNeverLandsOnIt()
    {
        await Expect(Minimap).ToHaveAttributeAsync("aria-hidden", "true");
        var empty = await EmptyCanvasPointAsync();
        await Page.Mouse.ClickAsync(empty.X, empty.Y);

        for (var i = 0; i < 12; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            Assert.False(await FocusIsInsideTheMinimapAsync());
        }

        for (var i = 0; i < 12; i++)
        {
            await Page.Keyboard.PressAsync("Shift+Tab");
            Assert.False(await FocusIsInsideTheMinimapAsync());
        }
    }

    [Fact]
    public async Task PanningTheCanvasMovesTheRect()
    {
        var before = await Rect.GetAttributeAsync("style");

        await StartMiddlePanAsync();
        await Page.Mouse.UpAsync(MiddleUp);
        await SettleAsync();

        await Expect(Rect).Not.ToHaveAttributeAsync("style", before!);
    }

    [Fact]
    public async Task DuplicatingAShapeAddsABox()
    {
        await Instance(NorthWestId).ClickAsync();
        await Page.Keyboard.PressAsync("Control+KeyD");

        await Expect(Page.Locator(".component-container")).ToHaveCountAsync(5);
        await Expect(Page.Locator(".d12-minimap-box")).ToHaveCountAsync(5);
    }
}
