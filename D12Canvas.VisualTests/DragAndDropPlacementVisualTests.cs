using System.Text.RegularExpressions;
using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baselines for drag-and-drop placement (ticket 27): the drag-in-progress
// affordance, and the board after a completed drop. Any later ticket that renders a new visual
// state on canvas should add a case here alongside its own.
public sealed class DragAndDropPlacementVisualTests : IAsyncLifetime
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
    public DragAndDropPlacementVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/placement-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(5);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task DragInProgress_MatchesBaseline()
    {
        var target = _page.Locator(".diagram-canvas");

        // Dispatched rather than driven by a live Mouse.Down/Move gesture: Chromium doesn't
        // repaint the page while an actual native HTML5 drag session is in flight, so a
        // screenshot taken mid-gesture captures a stale frame even though the underlying
        // DOM/CSS state is already correct. A synthetic dragover (with a real DataTransfer -
        // Blazor's event processing silently no-ops without one) reaches the identical
        // application state without that native-drag paint freeze.
        await _page.EvaluateAsync(
            """
            () => {
                const el = document.querySelector('.diagram-canvas');
                const dt = new DataTransfer();
                const event = new DragEvent('dragover', { bubbles: true, cancelable: true, dataTransfer: dt });
                el.dispatchEvent(event);
            }
            """
        );

        await Expect(target).ToHaveClassAsync(new Regex("drag-over"));

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task DroppedInstance_MatchesBaseline()
    {
        var source = _page.Locator(".d12-palette-entry-button").First;
        var target = _page.Locator(".diagram-canvas");

        await source.DragToAsync(
            target,
            new LocatorDragToOptions
            {
                TargetPosition = new TargetPosition { X = 400, Y = 300 },
            }
        );

        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(1);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
