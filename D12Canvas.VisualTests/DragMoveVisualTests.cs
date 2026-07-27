using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baselines for drag-move (ticket 30): a selected instance mid-drag, and the
// board after the drag is released. Any later ticket that renders a new visual state on canvas
// should add a case here alongside its own.
public sealed class DragMoveVisualTests : IAsyncLifetime
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
    public DragMoveVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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

    // Selects the sole placed instance and moves the real mouse to its center, ready for a
    // press/move/release sequence. Shared by both tests below.
    private async Task<(double X, double Y)> PlaceAndSelectInstance()
    {
        await _page.Locator(".d12-palette-entry-button").First.ClickAsync();
        var target = _page.Locator(".component-container");
        await target.ClickAsync();
        await Expect(target).ToHaveAttributeAsync("aria-selected", "true");

        var box = await target.BoundingBoxAsync();
        Assert.NotNull(box);
        var centerX = box!.X + box.Width / 2;
        var centerY = box.Y + box.Height / 2;
        await _page.Mouse.MoveAsync((float)centerX, (float)centerY);
        return (centerX, centerY);
    }

    [Fact]
    public async Task DragInProgress_MatchesBaseline()
    {
        var (startX, startY) = await PlaceAndSelectInstance();

        // A plain mouse drag (not native HTML5 drag-and-drop) - Chromium repaints normally while
        // it's in flight, so this can be driven with a real Mouse.Down/Move/Up sequence rather
        // than the synthetic-dragover workaround DragAndDropPlacementVisualTests needed.
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)(startX + 80), (float)(startY + 40));

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
    }

    [Fact]
    public async Task ReleasedInstance_MatchesBaseline()
    {
        var (startX, startY) = await PlaceAndSelectInstance();

        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)(startX + 80), (float)(startY + 40));
        await _page.Mouse.UpAsync();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
