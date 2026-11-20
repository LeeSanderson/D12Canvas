using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class ObjectSnappingVisualTests : IAsyncLifetime
{
    private const string MoverId = "c0000000-0000-0000-0000-000000000003";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

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
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/object-snapping-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(3);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Mover => _page.Locator($".component-container[data-d12-entity='{MoverId}']");

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
