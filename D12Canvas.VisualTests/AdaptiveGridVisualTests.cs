using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The adaptive multi-layer grid replaces the old single fixed-spacing grid - these cases span zoom
// levels in both directions, crossing at least one layer transition each way, so the grid stays
// legible far past the old prototype's fixed 0.6x-6x range instead of collapsing to a blur or
// stretching to a few barely-visible lines.
public sealed class AdaptiveGridVisualTests : IAsyncLifetime
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
    public AdaptiveGridVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/board-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(7);

        await _page
            .Locator(".diagram-canvas")
            .ClickAsync(
                new LocatorClickOptions
                {
                    Position = new Position { X = 5, Y = 5 },
                }
            );
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task ZoomedOutMidTransition_MatchesBaseline()
    {
        // Scale -> ~0.5: layer 0 (20 board units) and layer 1 (200 board units) both crossfading.
        for (var i = 0; i < 5; i++)
        {
            await _page.Keyboard.PressAsync("PageDown");
        }

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task ZoomedOutNextLayerDominant_MatchesBaseline()
    {
        // Scale -> ~0.1: layer 1 (200 board units) fully dominant - its on-screen spacing lands
        // back at the same legible ~20px the default zoom shows, unlike the old fixed grid, which
        // would already have collapsed to a near-solid blur this far out.
        for (var i = 0; i < 9; i++)
        {
            await _page.Keyboard.PressAsync("PageDown");
        }

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task ZoomedInMidTransition_MatchesBaseline()
    {
        // Scale -> ~1.5: layer 0 (20 board units) and layer -1 (2 board units) both crossfading.
        for (var i = 0; i < 5; i++)
        {
            await _page.Keyboard.PressAsync("PageUp");
        }

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task ZoomedInNextLayerDominant_MatchesBaseline()
    {
        // Scale -> 10.0 (same zoom level as BoardRenderingVisualTests.ExtremeZoomIn_MatchesBaseline):
        // layer -1 (2 board units) fully dominant, again landing back at the same legible ~20px
        // spacing rather than stretching to a few barely-visible lines.
        for (var i = 0; i < 90; i++)
        {
            await _page.Keyboard.PressAsync("PageUp");
        }

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
