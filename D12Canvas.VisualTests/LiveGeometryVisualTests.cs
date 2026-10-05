using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A shape pressed cold and dragged, held mid-drag: it is selected by the press, drawn where the
// gesture preview puts it, and the edge attached to it follows rather than waiting for release.
public sealed class LiveGeometryVisualTests : IAsyncLifetime
{
    private static readonly PageScreenshotOptions ScreenshotOptions = new()
    {
        FullPage = true,
        Type = ScreenshotType.Png,
        Animations = ScreenshotAnimations.Disabled,
    };

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
        var rectangle = await _page
            .Locator(".component-container[aria-label='Rectangle']")
            .BoundingBoxAsync();
        var stickyNote = await _page
            .Locator(".component-container[aria-label='Sticky Note']")
            .BoundingBoxAsync();
        Assert.NotNull(rectangle);
        Assert.NotNull(stickyNote);

        await _page.Mouse.MoveAsync(
            rectangle!.X + rectangle.Width / 2,
            rectangle.Y + rectangle.Height - 1
        );
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(stickyNote!.X + stickyNote.Width / 2, stickyNote.Y + 1);
        await _page.Mouse.UpAsync();
        await Expect(_page.Locator(".edge-line")).ToHaveCountAsync(1);
    }

    // Moves reach C# at most once per animation frame, so the last one can land after the mouse
    // call returns.
    private static async Task WaitUntilMovedPastAsync(ILocator locator, double leftOf)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var current = await locator.BoundingBoxAsync();
            if (current is not null && current.X < leftOf)
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail($"The dragged instance never moved left of x={leftOf}.");
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
        await WaitUntilMovedPastAsync(rectangle, box.X - 150);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);

        await _page.Mouse.UpAsync();
    }
}
