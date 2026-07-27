using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baselines for click-to-add placement (ticket 28): the board after a click-to-add,
// and after a second consecutive click showing the cascading offset. Any later ticket that renders a
// new visual state on canvas should add a case here alongside its own.
public sealed class ClickToAddPlacementVisualTests : IAsyncLifetime
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
    public ClickToAddPlacementVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(6);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task ClickToAddedInstance_MatchesBaseline()
    {
        await _page.Locator(".d12-palette-entry-button").First.ClickAsync();

        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(1);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task ConsecutiveClickToAdds_CascadeWithAnOffset_MatchesBaseline()
    {
        var entry = _page.Locator(".d12-palette-entry-button").First;
        await entry.ClickAsync();
        await entry.ClickAsync();

        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
