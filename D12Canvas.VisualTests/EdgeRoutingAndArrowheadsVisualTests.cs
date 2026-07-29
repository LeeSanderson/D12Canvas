using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baseline for ticket 52: per-edge routing style and arrowheads. The layered
// testing strategy (spec.md) keeps this Playwright layer small and curated - the combinatorial
// logic (every RoutingStyle x ArrowStyle pairing) is already exhaustively covered by
// DiagramCanvasEdgeRoutingAndArrowheadsTests (bUnit); this class only proves the real browser
// actually paints straight/orthogonal/curved routing and start/end/both/no arrowheads correctly,
// via EdgeStylesDemo's pre-seeded board (no interactive UI exists yet to set these - ticket 56, the
// property panel, isn't built).
public sealed class EdgeRoutingAndArrowheadsVisualTests : IAsyncLifetime
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
    public EdgeRoutingAndArrowheadsVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/edge-styles-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(10);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task StraightEdgesRenderAsLinesWithTheirOwnIndependentArrowCombination()
    {
        var lines = _page.Locator(".edges-layer > line.edge-line");
        await Expect(lines).ToHaveCountAsync(3);

        // Default: arrow at target only.
        await Expect(lines.Nth(0)).Not.ToHaveAttributeAsync("marker-start", "url(#edge-arrow)");
        await Expect(lines.Nth(0)).ToHaveAttributeAsync("marker-end", "url(#edge-arrow)");

        // Both ends arrowed.
        await Expect(lines.Nth(1)).ToHaveAttributeAsync("marker-start", "url(#edge-arrow)");
        await Expect(lines.Nth(1)).ToHaveAttributeAsync("marker-end", "url(#edge-arrow)");

        // Neither end arrowed.
        Assert.Null(await lines.Nth(2).GetAttributeAsync("marker-start"));
        Assert.Null(await lines.Nth(2).GetAttributeAsync("marker-end"));
    }

    [Fact]
    public async Task OrthogonalEdgeRendersAsARightAngledPath()
    {
        var paths = _page.Locator(".edges-layer > path.edge-line");
        await Expect(paths).ToHaveCountAsync(2);

        var d = await paths.Nth(0).GetAttributeAsync("d");
        Assert.NotNull(d);
        // Three segments (M + two L's + a final L) - the two right-angle bends.
        Assert.Equal(3, d!.Split(" L ").Length - 1);
    }

    [Fact]
    public async Task CurvedEdgeRendersAsASmoothPath()
    {
        var paths = _page.Locator(".edges-layer > path.edge-line");
        await Expect(paths).ToHaveCountAsync(2);

        var d = await paths.Nth(1).GetAttributeAsync("d");
        Assert.NotNull(d);
        Assert.Contains(" C ", d);
    }

    [Fact]
    public async Task AllRoutingStylesAndArrowheads_MatchBaseline()
    {
        await Expect(_page.Locator(".edges-layer > line.edge-line")).ToHaveCountAsync(3);
        await Expect(_page.Locator(".edges-layer > path.edge-line")).ToHaveCountAsync(2);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
