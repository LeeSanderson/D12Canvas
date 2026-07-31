using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class MultiSelectionMoveResizeVisualTests : IAsyncLifetime
{
    private static readonly PageScreenshotOptions ScreenshotOptions = new()
    {
        FullPage = true,
        Type = ScreenshotType.Png,
        Animations = ScreenshotAnimations.Disabled,
    };

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public MultiSelectionMoveResizeVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/placement-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(6);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    // Click-to-adds both palette entries and marquee-selects across both (the same technique
    // MarqueeVisualTests uses), leaving both instances selected with the group bounding box
    // visible.
    private async Task SelectBothInstances()
    {
        var entries = _page.Locator(".d12-palette-entry-button");
        await entries.Nth(0).ClickAsync();
        await entries.Nth(1).ClickAsync();
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);

        var boxes = await _page.Locator(".component-container").AllAsync();
        var first = await boxes[0].BoundingBoxAsync();
        var second = await boxes[1].BoundingBoxAsync();
        Assert.NotNull(first);
        Assert.NotNull(second);

        var from = (
            (float)Math.Min(first!.X, second!.X) - 20,
            (float)Math.Min(first.Y, second.Y) - 20
        );
        var to = (
            (float)Math.Max(first.X + first.Width, second.X + second.Width) + 20,
            (float)Math.Max(first.Y + first.Height, second.Y + second.Height) + 20
        );

        await _page.Mouse.MoveAsync(from.Item1, from.Item2);
        await _page.Keyboard.DownAsync("Shift");
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(to.Item1, to.Item2);
        await _page.Mouse.UpAsync();
        await _page.Keyboard.UpAsync("Shift");

        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);
    }

    [Fact]
    public async Task BoundingBoxVisible_MatchesBaseline()
    {
        await SelectBothInstances();

        await Expect(_page.Locator(".selection-bounding-box")).ToBeVisibleAsync();
        await Expect(_page.Locator(".group-resize-handle")).ToHaveCountAsync(8);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    // A persisted Group's bounding box (computed on demand from its members, never stored)
    // renders identically to an ad-hoc multi-selection's, since Ctrl+G only promotes the existing
    // selection into a Group entity without changing how it's drawn. Pinned as its own baseline
    // since it's driven by a real Ctrl+G keypress rather than the marquee/shift-click
    // SelectBothInstances itself already covers.
    [Fact]
    public async Task PersistedGroupBoundingBoxVisible_MatchesBaseline()
    {
        await SelectBothInstances();

        await _page.Keyboard.PressAsync("Control+g");

        await Expect(_page.Locator(".selection-bounding-box")).ToBeVisibleAsync();
        await Expect(_page.Locator(".group-resize-handle")).ToHaveCountAsync(8);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task GroupResizeInProgress_MatchesBaseline()
    {
        await SelectBothInstances();

        var handle = _page.Locator(".group-resize-handle.bottom-right");
        var box = await handle.BoundingBoxAsync();
        Assert.NotNull(box);
        var handleX = box!.X + box.Width / 2;
        var handleY = box.Y + box.Height / 2;

        await _page.Mouse.MoveAsync((float)handleX, (float)handleY);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)(handleX + 80), (float)(handleY + 40));

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
    }

    // Regression coverage for a real bug found and fixed here: a handle's own mousedown/mouseup
    // with no movement in between still fires a native click that bubbles past the
    // (pointer-events: none) bounding box up to the canvas's own click-to-clear-selection handler,
    // unless the handle also stops that click's propagation. bUnit can't drive this exact scenario
    // (see DiagramCanvasMultiSelectionMoveResizeTests's comment) - a real browser can.
    [Fact]
    public async Task StationaryClickOnGroupResizeHandle_DoesNotClearSelection()
    {
        await SelectBothInstances();

        var handle = _page.Locator(".group-resize-handle.bottom-right");
        var box = await handle.BoundingBoxAsync();
        Assert.NotNull(box);
        var handleX = box!.X + box.Width / 2;
        var handleY = box.Y + box.Height / 2;

        await _page.Mouse.MoveAsync((float)handleX, (float)handleY);
        await _page.Mouse.DownAsync();
        await _page.Mouse.UpAsync();

        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);
    }
}
