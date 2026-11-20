using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class PortDragVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public PortDragVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    private async Task NewPageAsync(ColorScheme colorScheme)
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/board-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(7);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Rectangle => _page.Locator(".component-container[aria-label='Rectangle']");

    private ILocator StickyNote => _page.Locator(".component-container[aria-label='Sticky Note']");

    private async Task DragHalfwayFromRectangleToStickyNote()
    {
        var from = await PortGestures.SelectAndAimAtPortAsync(Rectangle, "Bottom");
        var note = await StickyNote.BoundingBoxAsync();
        Assert.NotNull(note);
        var to = (X: note!.X + note.Width / 2, Y: note.Y);

        await _page.Mouse.MoveAsync(from.X, from.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync((float)((from.X + to.X) / 2), (float)((from.Y + to.Y) / 2));

        await Expect(_page.Locator(".connector-drag-preview")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task ConnectorDragInProgress_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await DragHalfwayFromRectangleToStickyNote();

        await ContentSnapshot.Verify(_page);

        await _page.Mouse.UpAsync();
    }

    [Fact]
    public async Task ConnectorDragInProgress_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await DragHalfwayFromRectangleToStickyNote();

        await ContentSnapshot.Verify(_page);

        await _page.Mouse.UpAsync();
    }

    [Fact]
    public async Task CarryingAnAttachedEndMidGesture_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await PortGestures.ConnectAsync(_page, Rectangle, "Bottom", StickyNote, "Top");
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);

        var to = await PortGestures.SelectAndAimAtPortAsync(StickyNote, "Top");
        var carriedTo = (X: to.X + 120, Y: to.Y + 30);
        await _page.Mouse.MoveAsync(to.X, to.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(carriedTo.X, carriedTo.Y, new() { Steps = 4 });

        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(0);
        await GestureWaits.UntilBoxAsync(
            _page.Locator(".connector-drag-preview"),
            box => Math.Abs(box.X + box.Width - carriedTo.X) < 4,
            "the pending line reaching the pointer"
        );

        await ContentSnapshot.Verify(_page);

        await _page.Mouse.UpAsync();
    }

    [Fact]
    public async Task ConnectedEdge_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);

        await PortGestures.ConnectAsync(_page, Rectangle, "Bottom", StickyNote, "Top");

        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);

        await ContentSnapshot.Verify(_page);
    }
}
