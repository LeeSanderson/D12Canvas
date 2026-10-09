using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The minimap demo floats a minimap over the canvas's bottom-right corner, the way the App places
// it. It opens with the initial fit, so the viewport rect sits over all four shapes; dragging the
// minimap to its top-right corner pans the canvas off the content and settles the map on the
// union of both. A dark theme set on an ancestor reaches the minimap as it reaches the canvas.
public sealed class MinimapVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public MinimapVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1200, Height = 800 },
            }
        );
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task<IPage> OpenAsync(string query = "")
    {
        var page = await _context.NewPageAsync();
        await page.GotoAsync("/minimap-demo" + query);
        await Expect(page.Locator(".component-container")).ToHaveCountAsync(4);
        await Expect(page.Locator(".d12-minimap-box")).ToHaveCountAsync(4);
        await Expect(page.Locator(".d12-minimap-viewport")).ToHaveCountAsync(1);
        return page;
    }

    [Fact]
    public async Task OpensWithABoxPerShapeAndTheViewportRect_MatchesBaseline()
    {
        var page = await OpenAsync();

        await ContentSnapshot.Verify(page);
    }

    [Fact]
    public async Task DarkThemeOnAnAncestor_MatchesBaseline()
    {
        var page = await OpenAsync("?theme=dark");

        Assert.Equal(
            "rgb(42, 42, 42)",
            await page.Locator(".d12-minimap")
                .EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor")
        );
        await ContentSnapshot.Verify(page);
    }

    [Fact]
    public async Task DraggedOffTheContent_MatchesBaseline()
    {
        var page = await OpenAsync();
        var minimap = await page.Locator(".d12-minimap").BoundingBoxAsync();
        Assert.NotNull(minimap);
        var mapBefore = await page.Locator(".d12-minimap-content").GetAttributeAsync("style");

        await page.Mouse.MoveAsync(minimap!.X + minimap.Width / 2, minimap.Y + minimap.Height / 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(
            minimap.X + minimap.Width - 4,
            minimap.Y + 4,
            new MouseMoveOptions { Steps = 6 }
        );
        await page.Mouse.UpAsync();

        await Expect(page.Locator(".d12-minimap-content"))
            .Not.ToHaveAttributeAsync("style", mapBefore!);
        await ContentSnapshot.Verify(page);
    }
}
