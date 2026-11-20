using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class MultiSelectionMoveResizeVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

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
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/placement-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(8);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task SelectBothInstances()
    {
        await PaletteEntry.Named(_page, PaletteEntry.DemoNote).ClickAsync();
        await PaletteEntry.Named(_page, PaletteEntry.StressItem).ClickAsync();
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
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(to.Item1, to.Item2);
        await _page.Mouse.UpAsync();

        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);
    }

    [Fact]
    public async Task BoundingBoxVisible_MatchesBaseline()
    {
        await SelectBothInstances();

        await Expect(_page.Locator(".selection-bounding-box")).ToBeVisibleAsync();
        await Expect(_page.Locator(".group-resize-handle")).ToHaveCountAsync(8);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PersistedGroupBoundingBoxVisible_MatchesBaseline()
    {
        await SelectBothInstances();

        await _page.Keyboard.PressAsync("Control+g");

        await Expect(_page.Locator(".selection-bounding-box")).ToBeVisibleAsync();
        await Expect(_page.Locator(".group-resize-handle")).ToHaveCountAsync(8);

        await ContentSnapshot.Verify(_page);
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
        var selectionBox = _page.Locator(".selection-bounding-box");
        var start = await selectionBox.BoundingBoxAsync();
        Assert.NotNull(start);

        await _page.Mouse.MoveAsync((float)handleX, (float)handleY);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)(handleX + 80), (float)(handleY + 40));
        await GestureWaits.UntilBoxAsync(
            selectionBox,
            current => current.Width > start!.Width + 40,
            "the selection growing mid-resize"
        );

        await ContentSnapshot.Verify(_page);

        await _page.Mouse.UpAsync();
    }

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
