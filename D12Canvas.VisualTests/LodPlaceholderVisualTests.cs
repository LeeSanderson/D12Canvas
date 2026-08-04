using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A dense, zoomed-out board swaps every instance's full component tree for the generic LOD
// placeholder once its on-screen size drops below the default threshold.
public sealed class LodPlaceholderVisualTests : IAsyncLifetime
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
                ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/board-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(7);
    }

    // Board demo's largest instance is 240x180 (the image built-in) - its larger dimension
    // crosses the default 32px LOD threshold once scale drops below 32/240 (~0.133). Nine
    // zoom-out notches (-0.1 each) lands at 0.1, well past that for every seeded instance.
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

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task DenseZoomedOutBoard_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await ZoomOutUntilEveryInstanceIsBelowLodThreshold();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
