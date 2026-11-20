using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class BoardRenderingVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

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
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/board-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(7);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task RenderedBoard_MatchesBaseline() => await ContentSnapshot.Verify(_page);

    [Fact]
    public async Task ZoomedAndPannedBoard_MatchesBaseline()
    {
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
            await _page.Keyboard.PressAsync("PageUp");
        }
        for (var i = 0; i < 6; i++)
        {
            await _page.Keyboard.PressAsync("ArrowRight");
        }
        for (var i = 0; i < 4; i++)
        {
            await _page.Keyboard.PressAsync("ArrowDown");
        }

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task ExtremeZoomIn_MatchesBaseline()
    {
        await _page
            .Locator(".diagram-canvas")
            .ClickAsync(
                new LocatorClickOptions
                {
                    Position = new Position { X = 5, Y = 5 },
                }
            );
        for (var i = 0; i < 90; i++)
        {
            await _page.Keyboard.PressAsync("PageUp");
        }

        await ContentSnapshot.Verify(_page);
    }
}
