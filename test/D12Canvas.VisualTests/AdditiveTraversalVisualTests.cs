using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The additive traversal demo board: two rectangles on a row and a third below them. Real keys
// select the first two through Space and Tab, then Tab on to the third, which is focused but not
// selected and so draws the dashed focus ring.
public sealed class AdditiveTraversalVisualTests : IAsyncLifetime
{
    private const string FirstId = "d0000000-0000-0000-0000-000000000001";
    private const string SecondId = "d0000000-0000-0000-0000-000000000002";
    private const string ThirdId = "d0000000-0000-0000-0000-000000000003";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public AdditiveTraversalVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/additive-traversal-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(3);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Instance(string id) =>
        _page.Locator($".component-container[data-d12-entity='{id}']");

    [Fact]
    public async Task FocusedUnselectedStopBesideTwoSelected_MatchesBaseline()
    {
        await Instance(FirstId).FocusAsync();
        await Expect(Instance(FirstId)).ToHaveAttributeAsync("aria-selected", "true");

        await _page.Keyboard.PressAsync("Space");
        await _page.Keyboard.PressAsync("Tab");
        await Expect(Instance(SecondId)).ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("Space");
        await Expect(Instance(SecondId)).ToHaveAttributeAsync("aria-selected", "true");
        await _page.Keyboard.PressAsync("Tab");

        await Expect(Instance(ThirdId)).ToBeFocusedAsync();
        await Expect(Instance(ThirdId)).Not.ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Instance(FirstId)).ToHaveAttributeAsync("aria-selected", "true");

        await _page.Mouse.MoveAsync(5, 5);
        await ContentSnapshot.Verify(_page);
    }
}
