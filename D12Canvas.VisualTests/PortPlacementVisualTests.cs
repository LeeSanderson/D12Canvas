using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The port placement demo's left rectangle with a keyboard port placement under way: Add port…
// chosen from the Shift+F10 menu and the provisional port slid one step left along the top side.
public sealed class PortPlacementVisualTests : IAsyncLifetime
{
    private const string ShapeId = "9d000000-0000-0000-0000-000000000001";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public PortPlacementVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/port-placement-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task ProvisionalPort_MatchesBaseline()
    {
        var shape = _page.Locator($".component-container[data-d12-entity='{ShapeId}']");
        await shape.FocusAsync();
        await Expect(shape).ToHaveAttributeAsync("aria-selected", "true");
        await _page.Keyboard.PressAsync("Shift+F10");
        var menu = _page.Locator(".d12-context-menu");
        await Expect(menu.Locator(".d12-context-menu-item").First).ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("ArrowUp");
        await Expect(menu.Locator(".d12-context-menu-item:focus .d12-context-menu-label"))
            .ToHaveTextAsync("Add port…");
        await _page.Keyboard.PressAsync("Enter");
        await Expect(menu).ToHaveCountAsync(0);
        await Expect(shape.Locator(".port-provisional")).ToHaveCountAsync(1);
        await Expect(shape).ToBeFocusedAsync();

        await _page.Keyboard.PressAsync("ArrowLeft");
        await Expect(shape.Locator(".port-provisional"))
            .ToHaveAttributeAsync(
                "style",
                new System.Text.RegularExpressions.Regex("left: calc\\(16\\.6")
            );
        await _page.Mouse.MoveAsync(5, 5);

        await ContentSnapshot.Verify(_page);
    }
}
