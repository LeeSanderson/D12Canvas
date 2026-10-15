using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// One edge leaving from each side of a shape into the top of a shape below and to its right, in
// each routed style: an orthogonal edge leaves straight out along its side and goes around both
// shapes, and a curved edge bends out along its side.
public sealed class EdgeRoutingSidesVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public EdgeRoutingSidesVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
                ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            }
        );
        _page = await _context.NewPageAsync();
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task OpenAsync(string routing)
    {
        await _page.GotoAsync($"/edge-styles-demo?sides={routing}");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(8);
        await Expect(_page.Locator(".edges-layer > path.edge-line")).ToHaveCountAsync(4);
    }

    [Fact]
    public async Task OrthogonalEdgesFromEachSide_MatchBaseline()
    {
        await OpenAsync("orthogonal");

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task CurvedEdgesFromEachSide_MatchBaseline()
    {
        await OpenAsync("curved");

        await ContentSnapshot.Verify(_page);
    }
}
