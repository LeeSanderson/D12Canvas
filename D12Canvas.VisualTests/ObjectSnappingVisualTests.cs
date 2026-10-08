using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The object snapping demo board: two rectangles on a row with a gap between them, and a third
// below them that the tests drag up beside the row. Snapping is help for a careful hand, so each
// drag arrives with one long jump, which the canvas sees as a fast pointer, and then settles with a
// small step after a pause.
public sealed class ObjectSnappingVisualTests : IAsyncLifetime
{
    private const string MoverId = "c0000000-0000-0000-0000-000000000003";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public ObjectSnappingVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/object-snapping-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(3);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Mover => _page.Locator($".component-container[data-d12-entity='{MoverId}']");

    // Presses the mover's centre and flings it far to the right and back in two long jumps, ending
    // one board unit short of where a careful hand would stop.
    private async Task<(float X, float Y)> FlingTheMoverBesideTheRowAsync()
    {
        var box = await Mover.BoundingBoxAsync();
        Assert.NotNull(box);
        var start = (X: box!.X + box.Width / 2, Y: box.Y + box.Height / 2);
        await _page.Mouse.MoveAsync(start.X, start.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(start.X + 300, start.Y - 177);
        await _page.Mouse.MoveAsync(start.X + 1, start.Y - 177);
        return start;
    }

    private async Task SettleSlowlyAsync((float X, float Y) start)
    {
        await Task.Delay(300, TestContext.Current.CancellationToken);
        await _page.Mouse.MoveAsync(start.X + 2, start.Y - 178);
    }

    [Fact]
    public async Task AFastPointerSeesNoGuideAndSlowingDownBringsItBack()
    {
        var start = await FlingTheMoverBesideTheRowAsync();
        await GestureWaits.UntilBoxAsync(
            Mover,
            box => box.Y < start.Y - 100,
            "the mover following the fling"
        );
        await Expect(_page.Locator(".alignment-guide")).ToHaveCountAsync(0);

        await SettleSlowlyAsync(start);

        await Expect(_page.Locator(".alignment-guide")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".spacing-guide")).ToHaveCountAsync(6);

        await _page.Mouse.UpAsync();
        await Expect(_page.Locator(".guide-strokes")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task GuideMidDrag_MatchesBaseline()
    {
        var start = await FlingTheMoverBesideTheRowAsync();
        await SettleSlowlyAsync(start);
        await Expect(_page.Locator(".alignment-guide")).ToHaveCountAsync(1);

        await ContentSnapshot.Verify(_page);

        await _page.Mouse.UpAsync();
    }
}
