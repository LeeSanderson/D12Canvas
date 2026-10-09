using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The same board on a light and a dark pane. An uncoloured edge paints in each pane's edge token,
// an authored colour paints the same on both, and selecting an edge draws a halo under it while
// its stroke and arrowheads keep their colour.
public sealed class EdgeColourVisualTests : IAsyncLifetime
{
    private const string LightEdge = "rgb(74, 74, 74)";
    private const string DarkEdge = "rgb(160, 160, 160)";
    private const string Authored = "rgb(229, 36, 107)";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public EdgeColourVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
                ViewportSize = new ViewportSize { Width = 1200, Height = 640 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/edge-colour-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(12);
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(6);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Pane(string theme) => _page.Locator($".theme-pane[data-d12-theme='{theme}']");

    private ILocator EdgeLine(string theme, int index) =>
        Pane(theme).Locator(".edge-line").Nth(index);

    private static Task<string> StrokeOf(ILocator element) =>
        element.EvaluateAsync<string>("el => getComputedStyle(el).stroke");

    private async Task SelectFirstTwoEdgesAsync(string theme)
    {
        await ClickCentreOfAsync(EdgeLine(theme, 0));
        await _page.Keyboard.DownAsync("Shift");
        await ClickCentreOfAsync(EdgeLine(theme, 1));
        await _page.Keyboard.UpAsync("Shift");
        await Expect(Pane(theme).Locator(".edge-halo")).ToHaveCountAsync(2);
    }

    private async Task ClickCentreOfAsync(ILocator element)
    {
        var box = await element.BoundingBoxAsync();
        Assert.NotNull(box);
        await _page.Mouse.ClickAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);
    }

    [Fact]
    public async Task AnUncolouredEdgePaintsInEachThemesEdgeToken()
    {
        Assert.Equal(LightEdge, await StrokeOf(EdgeLine("light", 0)));
        Assert.Equal(DarkEdge, await StrokeOf(EdgeLine("dark", 0)));
    }

    [Fact]
    public async Task AColouredEdgePaintsItsColourOnBothThemes()
    {
        Assert.Equal(Authored, await StrokeOf(EdgeLine("light", 1)));
        Assert.Equal(Authored, await StrokeOf(EdgeLine("dark", 1)));
    }

    [Fact]
    public async Task SelectingEdgesKeepsTheirStrokeColours()
    {
        await SelectFirstTwoEdgesAsync("light");
        await SelectFirstTwoEdgesAsync("dark");

        Assert.Equal(LightEdge, await StrokeOf(EdgeLine("light", 0)));
        Assert.Equal(Authored, await StrokeOf(EdgeLine("light", 1)));
        Assert.Equal(DarkEdge, await StrokeOf(EdgeLine("dark", 0)));
        Assert.Equal(Authored, await StrokeOf(EdgeLine("dark", 1)));
    }

    [Fact]
    public async Task EdgesOnBothThemes_MatchesBaseline() => await ContentSnapshot.Verify(_page);

    [Fact]
    public async Task SelectedEdgesShowAHaloOnBothThemes_MatchesBaseline()
    {
        await SelectFirstTwoEdgesAsync("light");
        await SelectFirstTwoEdgesAsync("dark");

        await ContentSnapshot.Verify(_page);
    }
}
