using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class EdgeSelectionVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public EdgeSelectionVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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

    private ILocator StickyNote => _page.Locator(".component-container[aria-label='Sticky Note']");

    // The seeded board's Rectangle sits directly above its Sticky Note (a 40px board-space gap), so
    // the Rectangle's bottom port to the note's top port is a short, deterministic drag. A straight
    // line's box is centred on the line's own midpoint, which is where it is clicked.
    private async Task<(float X, float Y)> ConnectRectangleToStickyNoteAsync()
    {
        await PortGestures.ConnectAsync(
            _page,
            _page.Locator(".component-container[aria-label='Rectangle']"),
            "Bottom",
            StickyNote,
            "Top"
        );
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);
        return await PortGestures.CentreOfAsync(_page.Locator(".edge-line"));
    }

    [Fact]
    public async Task SelectedEdge_MatchesBaseline()
    {
        var midpoint = await ConnectRectangleToStickyNoteAsync();

        await _page.Mouse.ClickAsync(midpoint.X, midpoint.Y);

        await Expect(_page.Locator(".edge-line")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(_page.Locator(".edge-halo")).ToHaveCountAsync(1);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task ShapeAndEdgeSelectedTogether_MatchesBaseline()
    {
        var midpoint = await ConnectRectangleToStickyNoteAsync();

        var box = await StickyNote.BoundingBoxAsync();
        Assert.NotNull(box);
        await _page.Mouse.ClickAsync(box!.X + box.Width / 2, box.Y + box.Height / 2);
        await Expect(StickyNote).ToHaveAttributeAsync("aria-selected", "true");

        await _page.Keyboard.DownAsync("Shift");
        await _page.Mouse.ClickAsync(midpoint.X, midpoint.Y);
        await _page.Keyboard.UpAsync("Shift");

        await Expect(_page.Locator(".edge-line")).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(StickyNote).ToHaveAttributeAsync("aria-selected", "true");

        await ContentSnapshot.Verify(_page);
    }
}
