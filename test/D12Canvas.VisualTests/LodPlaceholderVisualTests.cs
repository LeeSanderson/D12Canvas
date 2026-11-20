using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class LodPlaceholderVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public LodPlaceholderVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task NewPageAsync(ColorScheme colorScheme)
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/board-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(7);
    }

    private async Task ZoomOutUntilEveryInstanceIsBelowLodThreshold()
    {
        await _page
            .Locator(".diagram-canvas")
            .ClickAsync(
                new LocatorClickOptions
                {
                    Position = new Position { X = 5, Y = 5 },
                }
            );

        for (var i = 0; i < 9; i++)
        {
            await _page.Keyboard.PressAsync("PageDown");
        }

        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(0);
        await Expect(_page.Locator(".lod-placeholder")).ToHaveCountAsync(7);
    }

    [Fact]
    public async Task DenseZoomedOutBoard_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await ZoomOutUntilEveryInstanceIsBelowLodThreshold();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task DenseZoomedOutBoard_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await ZoomOutUntilEveryInstanceIsBelowLodThreshold();

        await ContentSnapshot.Verify(_page);
    }
}
