using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baselines for resize-via-handles (ticket 31): the resize-handle affordance on a
// selected instance, and a mid-resize state. Any later ticket that renders a new visual state on
// canvas should add a case here alongside its own.
public sealed class ResizeVisualTests : IAsyncLifetime
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
    public ResizeVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(4);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    // Places and selects the sole instance, returning the bounding box of its bottom-right
    // resize handle so callers can drive a real Mouse.Down/Move/Up sequence from its center.
    private async Task<(double X, double Y)> PlaceSelectAndLocateBottomRightHandle()
    {
        await _page.Locator(".d12-palette-entry-button").First.ClickAsync();
        var target = _page.Locator(".component-container");
        await target.ClickAsync();
        await Expect(target).ToHaveAttributeAsync("aria-selected", "true");

        var handle = _page.Locator(".resize-handle.bottom-right");
        await Expect(handle).ToBeVisibleAsync();
        var box = await handle.BoundingBoxAsync();
        Assert.NotNull(box);
        return (box!.X + box.Width / 2, box.Y + box.Height / 2);
    }

    [Fact]
    public async Task HandlesVisible_MatchesBaseline()
    {
        await PlaceSelectAndLocateBottomRightHandle();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task ResizeInProgress_MatchesBaseline()
    {
        var (handleX, handleY) = await PlaceSelectAndLocateBottomRightHandle();

        // A plain mouse drag (not native HTML5 drag-and-drop) - Chromium repaints normally while
        // it's in flight, matching DragMoveVisualTests' own approach for ticket 30.
        await _page.Mouse.MoveAsync((float)handleX, (float)handleY);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)(handleX + 80), (float)(handleY + 40));

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
    }
}
