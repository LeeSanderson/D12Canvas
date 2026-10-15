using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The border partition demo board: a small rectangle at the origin, one with six custom ports
// crowding its top side, and a large one. The partition itself is never drawn, so these show the
// dots and corner handles it leaves: on the crowded side, the ports squeezed below the floor are
// gone and the standard port keeps its place; zoomed out past a quarter, the dots and handles stop
// shrinking.
public sealed class BorderPartitionVisualTests : IAsyncLifetime
{
    private const string CrowdedId = "b0000000-0000-0000-0000-000000000002";
    private const string LargeId = "b0000000-0000-0000-0000-000000000003";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public BorderPartitionVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/border-partition-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(3);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Instance(string id) =>
        _page.Locator($".component-container[data-d12-entity='{id}']");

    private async Task<(float X, float Y)> CanvasPointAsync(double x, double y)
    {
        var box = await _page.Locator(".diagram-container").BoundingBoxAsync();
        Assert.NotNull(box);
        return ((float)(box!.X + x), (float)(box.Y + y));
    }

    [Fact]
    public async Task CrowdedSideOfASelectedShape_MatchesBaseline()
    {
        await Instance(CrowdedId).ClickAsync();

        await Expect(Instance(CrowdedId).Locator(".custom-port")).ToHaveCountAsync(4);
        await Expect(Instance(CrowdedId).Locator(".port-span")).ToHaveCountAsync(8);

        await ContentSnapshot.Verify(_page);
    }

    // The board opens fitted just below 100%; from 100%, eight zoom-out steps land at a fifth.
    [Fact]
    public async Task SelectedShapeBelowAQuarterZoom_MatchesBaseline()
    {
        var corner = await CanvasPointAsync(5, 550);
        await _page.Mouse.ClickAsync(corner.X, corner.Y);
        await _page.Keyboard.PressAsync("Shift+Digit0");
        await GestureWaits.UntilBoxAsync(
            Instance(LargeId),
            box => Math.Abs(box.Width - 340) < 1,
            "the large shape at 100%"
        );
        for (var i = 0; i < 8; i++)
        {
            await _page.Keyboard.PressAsync("PageDown");
        }

        var scale = await _page.EvaluateAsync<double>(
            "() => parseFloat(getComputedStyle(document.querySelector('.canvas-content')).getPropertyValue('--d12-scale'))"
        );
        Assert.True(scale < 0.25, $"Scale is {scale}.");
        await GestureWaits.UntilBoxAsync(
            Instance(LargeId),
            box => Math.Abs(box.Width - 340 * scale) < 1,
            "the large shape settling below a quarter of its size"
        );
        await Instance(LargeId).ClickAsync();

        await Expect(Instance(LargeId)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Instance(LargeId).Locator(".port")).ToHaveCountAsync(4);
        await Expect(Instance(LargeId).Locator(".resize-span")).ToHaveCountAsync(0);

        await ContentSnapshot.Verify(_page);
    }
}
