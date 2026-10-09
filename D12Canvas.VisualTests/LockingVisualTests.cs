using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The locking demo board: a free rectangle beside a locked one, with the property panel. A
// right-click on the locked one selects it, its menu reads Unlock, and the panel shows its fields
// disabled with the unlock control live.
public sealed class LockingVisualTests : IAsyncLifetime
{
    private const string LockedId = "10c00000-0000-0000-0000-000000000002";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public LockingVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/locking-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task RightClickOnALockedShapeOffersUnlockAndThePanelDisablesItsFields_MatchesBaseline()
    {
        await _page
            .Locator($".component-container[data-d12-entity='{LockedId}']")
            .ClickAsync(new() { Button = MouseButton.Right });

        await Expect(_page.Locator(".d12-context-menu")).ToBeVisibleAsync();
        await Expect(_page.Locator(".d12-property-panel-unlock")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }
}
