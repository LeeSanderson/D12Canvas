using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The entered group demo board: an outer group of one rectangle and a nested group of two, and
// one rectangle outside them. A double-click on the outer group's rectangle enters that group and
// selects the rectangle, and the dashed outline marks the outer group.
public sealed class EnteredGroupVisualTests : IAsyncLifetime
{
    private const string FirstId = "c0000000-0000-0000-0000-000000000001";
    private const string SecondId = "c0000000-0000-0000-0000-000000000002";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public EnteredGroupVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/entered-group-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(4);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task EnteredGroupWithAMemberSelected_MatchesBaseline()
    {
        var first = _page.Locator($".component-container[data-d12-entity='{FirstId}']");
        await first.DblClickAsync(
            new()
            {
                Position = new() { X = 30, Y = 30 },
            }
        );

        await Expect(_page.Locator(".entered-group-outline")).ToBeVisibleAsync();
        await Expect(first).Not.ToHaveAttributeAsync("data-d12-unaddressable", "true");
        await Expect(_page.Locator($".component-container[data-d12-entity='{SecondId}']"))
            .Not.ToHaveAttributeAsync("aria-selected", "true");
        await Expect(first).ToHaveAttributeAsync("aria-selected", "true");

        await _page.Mouse.MoveAsync(5, 5);
        await ContentSnapshot.Verify(_page);
    }
}
