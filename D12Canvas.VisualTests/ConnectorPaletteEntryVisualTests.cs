using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class ConnectorPaletteEntryVisualTests : IAsyncLifetime
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
    public ConnectorPaletteEntryVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
    public async Task DroppedConnector_MatchesBaseline()
    {
        var source = _page.Locator(".d12-palette-entry-button[aria-label='Connector']");
        var target = _page.Locator(".diagram-canvas");

        // Centered rather than nearer the container's edge: with the nav sidebar and palette
        // panel both sharing the page, .diagram-canvas's own viewport is narrower than the page
        // itself, and the dropped edge extends ConnectorDefaultHalfLength (40px) either side of
        // the drop point - a point too close to an edge would push one endpoint off-frame.
        await source.DragToAsync(
            target,
            new LocatorDragToOptions
            {
                TargetPosition = new TargetPosition { X = 150, Y = 250 },
            }
        );

        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".floating-endpoint")).ToHaveCountAsync(2);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
