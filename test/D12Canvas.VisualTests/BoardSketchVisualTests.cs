using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// One board with shapes, a sticky note, a labelled edge and a floating end, and one empty board,
// each sketched under a light and a dark data-d12-theme.
public sealed class BoardSketchVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public BoardSketchVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/board-sketch-demo");
        await Expect(_page.Locator(".d12-board-sketch")).ToHaveCountAsync(4);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task SketchesOnBothThemes_MatchesBaseline()
    {
        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task TheDarkThemeRepaintsTheSketch()
    {
        var notes = _page.Locator(".d12-board-sketch-note");
        var light = await notes.Nth(0).EvaluateAsync<string>("el => getComputedStyle(el).fill");
        var dark = await notes.Nth(1).EvaluateAsync<string>("el => getComputedStyle(el).fill");

        Assert.Equal("rgb(255, 244, 163)", light);
        Assert.Equal("rgb(77, 69, 32)", dark);
    }
}
