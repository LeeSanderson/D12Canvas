using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The grid and selection marquee are the first consumers of the shared theme-token layer: a light
// default, a dark default (driven by prefers-color-scheme), and a data-d12-theme override that
// takes precedence over the OS preference.
public sealed class ThemeVisualTests : IAsyncLifetime
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
    public ThemeVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    private async Task NewPageAsync(ColorScheme colorScheme)
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/placement-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(6);
    }

    // Places two instances close together and returns a from/to pair spanning both, ready to drag
    // a marquee across them - same technique MarqueeVisualTests already uses.
    private async Task<((float X, float Y) From, (float X, float Y) To)> PlaceTwoInstances()
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
        return (from, to);
    }

    private async Task DrawMarqueeAcross((float X, float Y) from, (float X, float Y) to)
    {
        await _page.Mouse.MoveAsync(from.X, from.Y);
        await _page.Keyboard.DownAsync("Shift");
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(to.X, to.Y);
        await Expect(_page.Locator(".marquee-select")).ToBeVisibleAsync();
    }

    private async Task<string> GridBackdropBackgroundColorAsync() =>
        await _page
            .Locator(".grid-backdrop")
            .EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

    [Fact]
    public async Task DarkColorScheme_UsesDarkGridSurfaceToken()
    {
        await NewPageAsync(ColorScheme.Dark);

        Assert.Equal("rgb(30, 30, 30)", await GridBackdropBackgroundColorAsync());
    }

    [Fact]
    public async Task DataThemeLight_OverridesADarkColorScheme()
    {
        await NewPageAsync(ColorScheme.Dark);

        await _page.EvaluateAsync("document.body.setAttribute('data-d12-theme', 'light')");

        Assert.Equal("rgb(240, 240, 240)", await GridBackdropBackgroundColorAsync());
    }

    [Fact]
    public async Task DataThemeDark_OverridesALightColorScheme()
    {
        await NewPageAsync(ColorScheme.Light);

        await _page.EvaluateAsync("document.body.setAttribute('data-d12-theme', 'dark')");

        Assert.Equal("rgb(30, 30, 30)", await GridBackdropBackgroundColorAsync());
    }

    [Fact]
    public async Task GridAndMarqueeInProgress_LightColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        var (from, to) = await PlaceTwoInstances();

        await DrawMarqueeAcross(from, to);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
        await _page.Keyboard.UpAsync("Shift");
    }

    [Fact]
    public async Task GridAndMarqueeInProgress_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        var (from, to) = await PlaceTwoInstances();

        await DrawMarqueeAcross(from, to);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
        await _page.Keyboard.UpAsync("Shift");
    }

    [Fact]
    public async Task GridAndMarqueeInProgress_DataThemeDarkOverridingLightColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await _page.EvaluateAsync("document.body.setAttribute('data-d12-theme', 'dark')");
        var (from, to) = await PlaceTwoInstances();

        await DrawMarqueeAcross(from, to);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
        await _page.Keyboard.UpAsync("Shift");
    }
}
