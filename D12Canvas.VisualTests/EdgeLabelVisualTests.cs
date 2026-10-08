using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class EdgeLabelVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public EdgeLabelVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
    public async Task LabelledEdge_MatchesBaseline()
    {
        await PortGestures.ConnectAsync(
            _page,
            _page.Locator(".component-container[aria-label='Rectangle']"),
            "Bottom",
            _page.Locator(".component-container[aria-label='Sticky Note']"),
            "Top"
        );
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);

        // A straight line's box is centred on the line's own midpoint, so a double-click there
        // lands on the line whatever its slope and adds the default (empty) Text label, opened
        // for typing.
        var midpoint = await PortGestures.CentreOfAsync(_page.Locator(".edge-line"));
        await _page.Mouse.DblClickAsync(midpoint.X, midpoint.Y);
        await Expect(_page.Locator(".edge-label textarea.d12-text-editor")).ToBeFocusedAsync();

        await _page.Locator(".edge-label textarea.d12-text-editor").FillAsync("Connects to");
        await _page.Locator(".edge-label textarea.d12-text-editor").BlurAsync();
        await Expect(_page.Locator(".edge-label p.d12-text")).ToHaveTextAsync("Connects to");

        await ContentSnapshot.Verify(_page);
    }
}
