using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class EdgeSelectionVisualTests : IAsyncLifetime
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
    public EdgeSelectionVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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

    // Same 1px-inward nudge as PortDragVisualTests/FloatingEndpointVisualTests - a real browser's
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

    // The seeded board's Rectangle sits directly above its Sticky Note (a 40px board-space gap) -
    // Rectangle's bottom port to Sticky Note's top port gives a short, deterministic drag, same
    // pairing PortDragVisualTests uses to create an edge.
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
    public async Task SelectedEdge_MatchesBaseline()
    {
        var (from, to) = await RectangleToStickyNotePorts();

        await _page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)to.X, (float)to.Y);
        await _page.Mouse.UpAsync();
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);

        // The line spans exactly from -> to (the same two port points used to draw it), so their
        // midpoint always lands on the line itself, regardless of slope - the same reasoning
        // PortDragVisualTests already relies on for its own mid-drag preview point.
        await _page.Mouse.ClickAsync((float)((from.X + to.X) / 2), (float)((from.Y + to.Y) / 2));

        await Expect(_page.Locator(".edge-line")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(_page.Locator(".edge-line")).ToHaveClassAsync("edge-line selected");

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
