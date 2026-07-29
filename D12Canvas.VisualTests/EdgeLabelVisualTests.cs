using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baseline for ticket 53: an edge label (ADR 0005) - added by double-clicking an
// edge's line, then edited in place exactly like any other Text built-in.
public sealed class EdgeLabelVisualTests : IAsyncLifetime
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
    public EdgeLabelVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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

    // Same 1px-inward nudge as PortDragVisualTests/EdgeSelectionVisualTests - a real browser's
    // hit-testing at the box's exact mathematical edge can resolve to whatever's behind the
    // element instead of the element itself.
    private static async Task<(double X, double Y)> BottomPortOf(ILocator container)
    {
        var box = await container.BoundingBoxAsync();
        Assert.NotNull(box);
        return (box!.X + box.Width / 2, box.Y + box.Height - 1);
    }

    private static async Task<(double X, double Y)> TopPortOf(ILocator container)
    {
        var box = await container.BoundingBoxAsync();
        Assert.NotNull(box);
        return (box!.X + box.Width / 2, box.Y + 1);
    }

    // The seeded board's Rectangle sits directly above its Sticky Note - same pairing
    // PortDragVisualTests/EdgeSelectionVisualTests use to create an edge via a port-to-port drag.
    private async Task<(
        (double X, double Y) From,
        (double X, double Y) To
    )> RectangleToStickyNotePorts()
    {
        var rectangle = _page.Locator(".component-container[aria-label='Rectangle']");
        var stickyNote = _page.Locator(".component-container[aria-label='Sticky Note']");

        return (await BottomPortOf(rectangle), await TopPortOf(stickyNote));
    }

    [Fact]
    public async Task LabelledEdge_MatchesBaseline()
    {
        var (from, to) = await RectangleToStickyNotePorts();

        await _page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)to.X, (float)to.Y);
        await _page.Mouse.UpAsync();
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);

        // The line spans exactly from -> to, so their midpoint always lands on the line itself,
        // regardless of slope (same reasoning PortDragVisualTests relies on for its own preview
        // point) - double-clicking there adds the default (empty) Text label.
        var midpoint = ((from.X + to.X) / 2, (from.Y + to.Y) / 2);
        await _page.Mouse.DblClickAsync((float)midpoint.Item1, (float)midpoint.Item2);
        await Expect(_page.Locator(".edge-label")).ToHaveCountAsync(1);

        // Give the label some visible text, matching how an end user would actually use it.
        await _page.Locator(".edge-label p.d12-text").DblClickAsync();
        await _page.Locator(".edge-label textarea.d12-text-editor").FillAsync("Connects to");
        await _page.Locator(".edge-label textarea.d12-text-editor").BlurAsync();
        await Expect(_page.Locator(".edge-label p.d12-text")).ToHaveTextAsync("Connects to");

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
