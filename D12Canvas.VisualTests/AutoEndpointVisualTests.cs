using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The auto endpoint demo board: two rectangles, the second below and to the right of the first.
// A connector pulled from the first's right port and dropped on the second's body attaches at the
// side of the second that faces the first, and keeps choosing as the second moves.
public sealed class AutoEndpointVisualTests : IAsyncLifetime
{
    private const string SourceId = "d0000000-0000-0000-0000-000000000001";
    private const string TargetId = "d0000000-0000-0000-0000-000000000002";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public AutoEndpointVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/auto-endpoint-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Instance(string id) =>
        _page.Locator($".component-container[data-d12-entity='{id}']");

    private async Task<(float X, float Y)> CentreOfAsync(string id)
    {
        var box = await Instance(id).BoundingBoxAsync();
        Assert.NotNull(box);
        return (box!.X + box.Width / 2, box.Y + box.Height / 2);
    }

    private async Task DropAConnectorOnTheTargetsBodyAsync()
    {
        var source = await Instance(SourceId).BoundingBoxAsync();
        Assert.NotNull(source);
        var from = (X: source!.X + source.Width - 1, Y: source.Y + source.Height / 2);
        var to = await CentreOfAsync(TargetId);

        await _page.Mouse.MoveAsync(from.X, from.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(to.X, to.Y, new() { Steps = 8 });
        await _page.Mouse.UpAsync();

        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".floating-endpoint")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task EdgeDroppedOnAShapesBody_MatchesBaseline()
    {
        await DropAConnectorOnTheTargetsBodyAsync();

        var line = _page.Locator(".edge-line");
        await Expect(line).ToHaveAttributeAsync("x2", "500");
        await Expect(line).ToHaveAttributeAsync("y2", "300");

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task KeyboardPortPickStartingOnAuto_MatchesBaseline()
    {
        await Instance(TargetId).FocusAsync();
        await _page.Keyboard.PressAsync("Enter");

        await Expect(Instance(TargetId).Locator(".port-focused")).ToHaveCountAsync(4);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task MovingTheTargetMakesTheEdgeLeaveFromTheSideNowFacingTheSource()
    {
        await DropAConnectorOnTheTargetsBodyAsync();

        var start = await CentreOfAsync(TargetId);
        await _page.Mouse.MoveAsync(start.X, start.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(start.X + 100, start.Y - 200, new() { Steps = 8 });
        await _page.Mouse.UpAsync();

        var line = _page.Locator(".edge-line");
        await Expect(line).ToHaveAttributeAsync("x2", "520");
        await Expect(line).ToHaveAttributeAsync("y2", "150");
    }
}
