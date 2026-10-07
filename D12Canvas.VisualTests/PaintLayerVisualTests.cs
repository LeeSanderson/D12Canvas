using System.Text.RegularExpressions;
using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The paint layers demo board, at its untouched pan and zoom, so a board point is a canvas point.
// What the browser hit-tests at a point is the topmost painted element there, so each hit check
// asserts paint order and hit order at once.
public sealed class PaintLayerVisualTests : IAsyncLifetime
{
    private const string CrossingId = "b0000000-0000-0000-0000-000000000003";
    private const string HighId = "b0000000-0000-0000-0000-000000000004";

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
    public PaintLayerVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/paint-layers-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(5);
        await Expect(_page.Locator(".edge-label")).ToHaveCountAsync(1);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task TheFirstPlacedShapePaintsAboveTheEdgeAndLabelBeneathIt()
    {
        var label = await CentreOfAsync(_page.Locator(".edge-label"));

        Assert.Equal(("instance", CrossingId), await HitAtAsync(300, 205));
        Assert.Equal(("instance", CrossingId), await HitAtPageAsync(label.X, label.Y));
    }

    [Fact]
    public async Task AShapeSentToBackStillPaintsAboveTheEdgeAndLabelBeneathIt_MatchesBaseline()
    {
        var crossing = _page.Locator($".component-container[data-d12-entity='{CrossingId}']");
        await crossing.ClickAsync(
            new()
            {
                Position = new() { X = 30, Y = 80 },
            }
        );
        await Expect(crossing).ToHaveAttributeAsync("aria-selected", "true");

        await _page.Keyboard.PressAsync("Control+Shift+[");
        await Expect(crossing).ToHaveAttributeAsync("style", new Regex("z-index: -"));
        await _page.Keyboard.PressAsync("Escape");
        await Expect(crossing).Not.ToHaveAttributeAsync("aria-selected", "true");

        Assert.Equal(("instance", CrossingId), await HitAtAsync(300, 205));
        var label = await CentreOfAsync(_page.Locator(".edge-label"));
        Assert.Equal(("instance", CrossingId), await HitAtPageAsync(label.X, label.Y));

        await _page.Mouse.MoveAsync(5, 5);
        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task AnEdgeOverEmptyCanvasTakesThePress()
    {
        var (role, _) = await HitAtAsync(225, 205);

        Assert.Equal("edge", role);
    }

    [Fact]
    public async Task AFloatingEndpointOverAShapeWithAHighZIndexTakesThePress()
    {
        var (role, _) = await HitAtAsync(340, 390);

        Assert.Equal("edge-endpoint", role);
    }

    [Fact]
    public async Task TheSelectionBoxAndHandlesPaintAboveShapesWithAHighZIndex()
    {
        await DragOnCanvasAsync((250, 320), (650, 470));
        var box = _page.Locator(".selection-bounding-box");
        await Expect(box).ToBeVisibleAsync();

        var handle = await CentreOfAsync(_page.Locator(".group-resize-handle.top-left"));
        Assert.Equal("selection-handle", (await HitAtPageAsync(handle.X, handle.Y)).Role);
        var inside = await CentreOfAsync(
            _page.Locator($".component-container[data-d12-entity='{HighId}']")
        );
        Assert.Equal("selection-bounds", (await HitAtPageAsync(inside.X + 40, inside.Y)).Role);
    }

    private async Task DragOnCanvasAsync((double X, double Y) from, (double X, double Y) to)
    {
        var start = await PagePointAsync(from.X, from.Y);
        var end = await PagePointAsync(to.X, to.Y);
        await _page.Mouse.MoveAsync(start.X, start.Y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(end.X, end.Y, new() { Steps = 6 });
        await _page.Mouse.UpAsync();
    }

    private async Task<(string? Role, string? EntityId)> HitAtAsync(double x, double y)
    {
        var point = await PagePointAsync(x, y);
        return await HitAtPageAsync(point.X, point.Y);
    }

    private async Task<(string? Role, string? EntityId)> HitAtPageAsync(float x, float y)
    {
        var hit = await _page.EvaluateAsync<string?[]>(
            """
            ({ x, y }) => {
                const marked = document.elementFromPoint(x, y)?.closest("[data-d12-role]");
                return [
                    marked?.getAttribute("data-d12-role") ?? null,
                    marked?.closest("[data-d12-entity]")?.getAttribute("data-d12-entity") ?? null
                ];
            }
            """,
            new { x = (double)x, y = (double)y }
        );
        return (hit[0], hit[1]);
    }

    private async Task<(float X, float Y)> PagePointAsync(double x, double y)
    {
        var box = await _page.Locator(".diagram-canvas").BoundingBoxAsync();
        Assert.NotNull(box);
        return ((float)(box!.X + x), (float)(box.Y + y));
    }

    private static async Task<(float X, float Y)> CentreOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        return ((float)(box!.X + box.Width / 2), (float)(box.Y + box.Height / 2));
    }
}
