using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class EdgeRoutingAndArrowheadsVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

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
                ReducedMotion = ReducedMotion.Reduce,
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

        await Expect(lines.Nth(0)).Not.ToHaveAttributeAsync("marker-start", "url(#edge-arrow)");
        await Expect(lines.Nth(0)).ToHaveAttributeAsync("marker-end", "url(#edge-arrow)");

        await Expect(lines.Nth(1)).ToHaveAttributeAsync("marker-start", "url(#edge-arrow)");
        await Expect(lines.Nth(1)).ToHaveAttributeAsync("marker-end", "url(#edge-arrow)");

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

        await ContentSnapshot.Verify(_page);
    }
}
