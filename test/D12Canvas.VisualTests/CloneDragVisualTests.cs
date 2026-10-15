using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The clone drag demo board: two rectangles side by side joined by an edge. Both are selected and
// dragged down, and Alt pressed mid-drag puts copies of both and their edge under the pointer with
// the selected look, while the originals show unselected where they were.
public sealed class CloneDragVisualTests : IAsyncLifetime
{
    private const string LeftId = "e0000000-0000-0000-0000-000000000001";
    private const string RightId = "e0000000-0000-0000-0000-000000000002";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public CloneDragVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/clone-drag-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Instance(string id) =>
        _page.Locator($".component-container[data-d12-entity='{id}']");

    [Fact]
    public async Task CloneInProgress_MatchesBaseline()
    {
        await Instance(LeftId).ClickAsync();
        await Instance(RightId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);

        var box = await Instance(LeftId).BoundingBoxAsync();
        Assert.NotNull(box);
        var startX = box!.X + box.Width / 2;
        var startY = box.Y + box.Height / 2;
        await _page.Mouse.MoveAsync(startX, startY);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(startX, startY + 200, new() { Steps = 8 });
        await _page.Keyboard.DownAsync("Alt");

        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(4);
        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);
        await Expect(Instance(LeftId)).ToHaveAttributeAsync("style", new Regex("top: 80px;"));

        await ContentSnapshot.Verify(_page);

        await _page.Mouse.UpAsync();
        await _page.Keyboard.UpAsync("Alt");
    }
}
