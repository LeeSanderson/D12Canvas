using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class CustomPortVisualTests : IAsyncLifetime
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
    public CustomPortVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private static async Task<(double X, double Y)> TopPortOf(ILocator container)
    {
        var box = await container.BoundingBoxAsync();
        Assert.NotNull(box);
        return (box!.X + box.Width / 2, box.Y + 1);
    }

    private static async Task<(double X, double Y)> CenterOf(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        return (box!.X + box.Width / 2, box.Y + box.Height / 2);
    }

    [Fact]
    public async Task InstanceWithACustomPortAndAttachedEdge_MatchesBaseline()
    {
        var rectangle = _page.Locator(".component-container[aria-label='Rectangle']");
        await rectangle.ClickAsync();

        // Double-click 75% of the way down the left border strip - away from the standard
        // left port's own border-center spot - adds a custom port there (fraction (0, 0.75)).
        var strip = rectangle.Locator(".port-strip-left");
        await Expect(strip).ToHaveCountAsync(1);
        var stripBox = await strip.BoundingBoxAsync();
        Assert.NotNull(stripBox);
        await strip.DblClickAsync(
            new LocatorDblClickOptions
            {
                Position = new Position
                {
                    X = stripBox!.Width / 2,
                    Y = (float)(stripBox.Height * 0.75),
                },
            }
        );
        await Expect(_page.Locator(".custom-port")).ToHaveCountAsync(1);

        // Drag from the new custom port to the Sticky Note's top port - the same port-to-port
        // gesture PortDragVisualTests uses, just starting from a custom port instead of a
        // standard one.
        var (fromX, fromY) = await CenterOf(_page.Locator(".custom-port"));
        var (toX, toY) = await TopPortOf(
            _page.Locator(".component-container[aria-label='Sticky Note']")
        );
        await _page.Mouse.MoveAsync((float)fromX, (float)fromY);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)toX, (float)toY);
        await _page.Mouse.UpAsync();

        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
