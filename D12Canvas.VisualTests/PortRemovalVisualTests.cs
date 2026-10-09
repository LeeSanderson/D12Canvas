using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The port removal demo's left rectangle with its object menu opened by a right-click on its
// custom port's span, showing Remove port where Add port here would otherwise be.
public sealed class PortRemovalVisualTests : IAsyncLifetime
{
    private const string ShapeId = "9e000000-0000-0000-0000-000000000001";
    private const string CustomPortId = "9e000000-0000-0000-0000-0000000000c1";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public PortRemovalVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/port-removal-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task RemovePortMenu_MatchesBaseline()
    {
        var shape = _page.Locator($".component-container[data-d12-entity='{ShapeId}']");
        await shape.ClickAsync();
        var span = shape.Locator($".port-span[data-d12-part='{CustomPortId}']");
        await Expect(span).ToHaveCountAsync(1);
        var box = await span.BoundingBoxAsync();
        Assert.NotNull(box);

        await _page.Mouse.ClickAsync(
            box!.X + box.Width / 2,
            box.Y + box.Height / 2,
            new() { Button = MouseButton.Right }
        );
        var menu = _page.Locator(".d12-context-menu");
        await Expect(menu.GetByText("Remove port")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }
}
