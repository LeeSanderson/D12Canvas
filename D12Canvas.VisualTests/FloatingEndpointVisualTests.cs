using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class FloatingEndpointVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public FloatingEndpointVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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

    [Fact]
    public async Task EdgeWithAFloatingEndpoint_MatchesBaseline()
    {
        var rectangle = _page.Locator(".component-container[aria-label='Rectangle']");
        var from = await PortGestures.SelectAndAimAtPortAsync(rectangle, "Bottom");

        // The seeded board (BoardDemo.razor) has nothing at board-space x < 120 - every seeded
        // instance starts at x >= 120. At this page's untouched default pan (0,0) and scale (1),
        // board space maps directly onto page space offset only by the container's own page
        // position, so a point 60px right of the container's own left edge is guaranteed to land
        // on empty canvas regardless of y, without needing to reason about every instance's exact
        // bounds individually.
        var containerBox = await _page.Locator(".diagram-canvas").BoundingBoxAsync();
        Assert.NotNull(containerBox);
        var to = (X: containerBox!.X + 60, Y: containerBox.Y + 300);

        await _page.Mouse.MoveAsync(from.X, from.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)to.X, (float)to.Y);
        await _page.Mouse.UpAsync();

        await Expect(_page.Locator(".floating-endpoint")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);

        await ContentSnapshot.Verify(_page);
    }
}
