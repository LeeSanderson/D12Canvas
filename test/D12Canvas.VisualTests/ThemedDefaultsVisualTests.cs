using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A rectangle and a text with no authored colour follow each pane's board tokens, so the same
// board reads correctly under both themes, while an authored colour is taken literally on both.
public sealed class ThemedDefaultsVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public ThemedDefaultsVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
                ViewportSize = new ViewportSize { Width = 1200, Height = 500 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/themed-defaults-demo");
        await Expect(_page.Locator(".d12-text")).ToHaveCountAsync(4);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task<string> ColorOfNth(string selector, int index) =>
        await _page
            .Locator(selector)
            .Nth(index)
            .EvaluateAsync<string>("el => getComputedStyle(el).color");

    [Fact]
    public async Task UnauthoredTextReadsEachPanesBoardTextToken()
    {
        Assert.Equal("rgb(0, 0, 0)", await ColorOfNth(".d12-text", 0));
        Assert.Equal("rgb(232, 232, 232)", await ColorOfNth(".d12-text", 2));
    }

    [Fact]
    public async Task AuthoredTextKeepsItsColourOnBothPanes()
    {
        Assert.Equal("rgb(21, 101, 192)", await ColorOfNth(".d12-text", 1));
        Assert.Equal("rgb(21, 101, 192)", await ColorOfNth(".d12-text", 3));
    }

    [Fact]
    public async Task ThemedDefaultsOnBothPanes_MatchesBaseline() =>
        await ContentSnapshot.Verify(_page);
}
