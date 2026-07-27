using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baselines for marquee + shift-click multi-select (ticket 32): the marquee
// rectangle mid-drag, and the resulting multi-selection once released. Any later ticket that
// renders a new visual state on canvas should add a case here alongside its own.
public sealed class MarqueeVisualTests : IAsyncLifetime
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
    public MarqueeVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(5);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    // Click-to-adds both palette entries so they land close together (viewport-centre plus the
    // small cascading offset from ticket 28), then returns a point above/left of both and a point
    // below/right of both, ready to drag a marquee across the pair.
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

    [Fact]
    public async Task MarqueeInProgress_MatchesBaseline()
    {
        var (from, to) = await PlaceTwoInstances();

        // A plain drag pans the canvas (pre-existing behaviour) - Shift+drag draws the marquee
        // instead, same real Mouse.Down/Move technique DragMoveVisualTests uses for ticket 30.
        await _page.Mouse.MoveAsync(from.X, from.Y);
        await _page.Keyboard.DownAsync("Shift");
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(to.X, to.Y);

        await Expect(_page.Locator(".marquee-select")).ToBeVisibleAsync();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
        await _page.Keyboard.UpAsync("Shift");
    }

    [Fact]
    public async Task BothInstancesSelected_AfterMarqueeRelease_MatchesBaseline()
    {
        var (from, to) = await PlaceTwoInstances();

        await _page.Mouse.MoveAsync(from.X, from.Y);
        await _page.Keyboard.DownAsync("Shift");
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(to.X, to.Y);
        await _page.Mouse.UpAsync();
        await _page.Keyboard.UpAsync("Shift");

        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
