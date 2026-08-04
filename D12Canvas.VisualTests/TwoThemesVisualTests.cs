using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Two independently-mounted DiagramCanvas+Palette pairs, each under its own data-d12-theme
// override, on a single page - proving ADR 0012's core promise that two chrome instances can
// carry different themes simultaneously, with no shared global state.
public sealed class TwoThemesVisualTests : IAsyncLifetime
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
    public TwoThemesVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1400, Height = 700 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/two-themes-demo");
        await Expect(_page.Locator(".d12-palette")).ToHaveCountAsync(2);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task<string> BackgroundColorOfNth(string selector, int index) =>
        await _page
            .Locator(selector)
            .Nth(index)
            .EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");

    [Fact]
    public async Task LightPaneReadsLightTokensRegardlessOfTheDarkPane()
    {
        Assert.Equal("rgb(240, 240, 240)", await BackgroundColorOfNth(".grid-backdrop", 0));
        Assert.Equal("rgb(255, 255, 255)", await BackgroundColorOfNth(".d12-palette", 0));
    }

    [Fact]
    public async Task DarkPaneReadsDarkTokensRegardlessOfTheLightPane()
    {
        Assert.Equal("rgb(30, 30, 30)", await BackgroundColorOfNth(".grid-backdrop", 1));
        Assert.Equal("rgb(42, 42, 42)", await BackgroundColorOfNth(".d12-palette", 1));
    }

    [Fact]
    public async Task TwoIndependentlyThemedPanes_MatchesBaseline() =>
        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
}
