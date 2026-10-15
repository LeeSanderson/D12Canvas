using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A shape held mid-drag or mid-resize is drawn where the gesture preview puts it, and the edge
// attached to it follows rather than waiting for release. A cold drag is selected by its press.
public sealed class LiveGeometryVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public LiveGeometryVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
                ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/board-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(7);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    // The seeded board's Rectangle sits directly above its Sticky Note, so its bottom port to the
    // note's top port is a short, deterministic connector drag.
    private async Task ConnectRectangleToStickyNoteAsync()
    {
        await PortGestures.ConnectAsync(
            _page,
            _page.Locator(".component-container[aria-label='Rectangle']"),
            "Bottom",
            _page.Locator(".component-container[aria-label='Sticky Note']"),
            "Top"
        );
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task AttachedEdgeFollowsAColdDragMidGesture_MatchesBaseline()
    {
        await ConnectRectangleToStickyNoteAsync();
        var rectangle = _page.Locator(".component-container[aria-label='Rectangle']");
        await Expect(rectangle).Not.ToHaveAttributeAsync("aria-selected", "true");
        var box = await rectangle.BoundingBoxAsync();
        Assert.NotNull(box);
        var startX = box!.X + box.Width / 2;
        var startY = box.Y + box.Height / 2;

        await _page.Mouse.MoveAsync(startX, startY);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(startX - 200, startY + 330, new() { Steps = 4 });
        await Expect(rectangle).ToHaveAttributeAsync("aria-selected", "true");
        await GestureWaits.UntilBoxAsync(
            rectangle,
            current => current.X < box.X - 150,
            $"the dragged instance left of x={box.X - 150}"
        );

        await ContentSnapshot.Verify(_page);

        await _page.Mouse.UpAsync();
    }

    [Fact]
    public async Task AttachedEdgeFollowsAResizeMidGesture_MatchesBaseline()
    {
        await ConnectRectangleToStickyNoteAsync();
        var rectangle = _page.Locator(".component-container[aria-label='Rectangle']");
        await rectangle.ClickAsync();
        await Expect(rectangle).ToHaveAttributeAsync("aria-selected", "true");
        var box = await rectangle.BoundingBoxAsync();
        var handle = await rectangle.Locator(".resize-span-left").First.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.NotNull(handle);
        var startX = handle!.X + handle.Width / 2;
        var startY = handle.Y + handle.Height / 2;

        await _page.Mouse.MoveAsync(startX, startY);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(startX - 200, startY, new() { Steps = 4 });
        await GestureWaits.UntilBoxAsync(
            rectangle,
            current => current.Width > box!.Width + 120,
            "the rectangle growing mid-resize"
        );

        await ContentSnapshot.Verify(_page);

        await _page.Mouse.UpAsync();
    }
}
