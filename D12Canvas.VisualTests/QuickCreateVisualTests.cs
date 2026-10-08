using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The quick create demo board's rectangle, chained twice by clicking the right port of the newest
// shape: three rectangles in a row, each joined to the next, with the last selected and focused.
public sealed class QuickCreateVisualTests : IAsyncLifetime
{
    private const string RectangleId = "9c000000-0000-0000-0000-000000000001";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public QuickCreateVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/quick-create-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Selected => _page.Locator(".component-container[aria-selected='true']");

    private async Task ClickRightPortOfAsync(ILocator shape)
    {
        var port = await PortGestures.SelectAndAimAtPortAsync(shape, "Right");
        await _page.Mouse.ClickAsync(port.X, port.Y);
    }

    [Fact]
    public async Task ChainOfThree_MatchesBaseline()
    {
        await ClickRightPortOfAsync(
            _page.Locator($".component-container[data-d12-entity='{RectangleId}']")
        );
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(3);
        await Expect(Selected).ToBeFocusedAsync();

        await ClickRightPortOfAsync(Selected);
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(4);
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(2);
        await Expect(Selected).ToBeFocusedAsync();

        await _page.Mouse.MoveAsync(5, 5);

        await ContentSnapshot.Verify(_page);
    }
}
