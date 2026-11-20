using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class AdaptiveGridVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

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
                ReducedMotion = ReducedMotion.Reduce,
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
        for (var i = 0; i < 5; i++)
        {
            await _page.Keyboard.PressAsync("PageDown");
        }

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task ZoomedOutNextLayerDominant_MatchesBaseline()
    {
        for (var i = 0; i < 9; i++)
        {
            await _page.Keyboard.PressAsync("PageDown");
        }

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task ZoomedInMidTransition_MatchesBaseline()
    {
        for (var i = 0; i < 5; i++)
        {
            await _page.Keyboard.PressAsync("PageUp");
        }

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task ZoomedInNextLayerDominant_MatchesBaseline()
    {
        for (var i = 0; i < 90; i++)
        {
            await _page.Keyboard.PressAsync("PageUp");
        }

        await ContentSnapshot.Verify(_page);
    }
}
