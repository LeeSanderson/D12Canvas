using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baselines for rendered board content (ticket 23). Any later ticket that renders
// a new visual state on canvas should add a case here alongside its own.
public sealed class BoardRenderingVisualTests : IAsyncLifetime
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
    public BoardRenderingVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(3);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task RenderedBoard_MatchesBaseline() =>
        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

    [Fact]
    public async Task ZoomedAndPannedBoard_MatchesBaseline()
    {
        // Focus the canvas without landing on a component instance, then drive the existing
        // keyboard shortcuts (DiagramCanvas.razor.js) rather than raw mouse coordinates.
        await _page
            .Locator(".diagram-canvas")
            .ClickAsync(
                new LocatorClickOptions
                {
                    Position = new Position { X = 5, Y = 5 },
                }
            );
        for (var i = 0; i < 5; i++)
        {
            await _page.Keyboard.PressAsync("PageUp"); // zoom in
        }
        for (var i = 0; i < 6; i++)
        {
            await _page.Keyboard.PressAsync("ArrowRight"); // pan
        }
        for (var i = 0; i < 4; i++)
        {
            await _page.Keyboard.PressAsync("ArrowDown"); // pan
        }

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
