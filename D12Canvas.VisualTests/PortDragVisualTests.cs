using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class PortDragVisualTests : IAsyncLifetime
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
    public PortDragVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

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

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    // Nudged 1px inward from the box's exact mathematical edge - a real browser's hit-testing at
    // that knife's-edge boundary can resolve to whatever's behind the element instead of the
    // element itself (sub-pixel rendering; getBoundingClientRect's own reported edge isn't always
    // hit-testable at that exact coordinate). 1px is trivially still inside the port's own 20px
    // hit circle (ComponentContainer.razor), so this doesn't change which port is targeted.
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
    // Rectangle's bottom port to Sticky Note's top port gives a short, deterministic drag.
    private async Task<(
        (double X, double Y) From,
        (double X, double Y) To
    )> RectangleToStickyNotePorts()
    {
        var rectangle = _page.Locator(".component-container[aria-label='Rectangle']");
        var stickyNote = _page.Locator(".component-container[aria-label='Sticky Note']");

        return (await BottomPortOf(rectangle), await TopPortOf(stickyNote));
    }

    // A plain mouse drag (not native HTML5 drag-and-drop) - same reasoning as
    // DragMoveVisualTests. Stops halfway rather than at the target port, so this is
    // unambiguously the in-progress preview and not the completed edge.
    private async Task DragHalfwayBetweenPorts()
    {
        var (from, to) = await RectangleToStickyNotePorts();

        await _page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)((from.X + to.X) / 2), (float)((from.Y + to.Y) / 2));

        await Expect(_page.Locator(".connector-drag-preview")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ConnectorDragInProgress_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await DragHalfwayBetweenPorts();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
    }

    [Fact]
    public async Task ConnectorDragInProgress_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await DragHalfwayBetweenPorts();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
    }

    [Fact]
    public async Task ConnectedEdge_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        var (from, to) = await RectangleToStickyNotePorts();

        await _page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)to.X, (float)to.Y);
        await _page.Mouse.UpAsync();

        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".connector-drag-preview")).ToHaveCountAsync(0);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
