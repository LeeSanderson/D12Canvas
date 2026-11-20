using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class PortsVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public PortsVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
        await _page.GotoAsync("/placement-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(8);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task SelectedShapeShowsItsPorts_MatchesBaseline()
    {
        await _page.Locator("button[aria-label='Rectangle']").ClickAsync();
        var placed = _page.Locator(".component-container");
        await Expect(placed).ToHaveAttributeAsync("aria-selected", "true");

        await Expect(placed.Locator(".port")).ToHaveCountAsync(4);
        await Expect(placed.Locator(".resize-handle")).ToHaveCountAsync(4);

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task PortsSitAtEachInstancesOwnBorderCenters()
    {
        foreach (var entry in new[] { "Rectangle", "Image" })
        {
            await _page.Locator($"button[aria-label='{entry}']").ClickAsync();
            var instance = _page.Locator(
                $".component-container[aria-label='{entry}'][aria-selected='true']"
            );
            await Expect(instance).ToHaveCountAsync(1);
            var box = await instance.BoundingBoxAsync();
            Assert.NotNull(box);

            await AssertPortCenteredAt(
                instance.Locator(".port-top"),
                box!.X + box.Width / 2,
                box.Y
            );
            await AssertPortCenteredAt(
                instance.Locator(".port-right"),
                box.X + box.Width,
                box.Y + box.Height / 2
            );
            await AssertPortCenteredAt(
                instance.Locator(".port-bottom"),
                box.X + box.Width / 2,
                box.Y + box.Height
            );
            await AssertPortCenteredAt(
                instance.Locator(".port-left"),
                box.X,
                box.Y + box.Height / 2
            );
        }
    }

    private static async Task AssertPortCenteredAt(
        ILocator port,
        double expectedX,
        double expectedY
    )
    {
        var box = await port.BoundingBoxAsync();
        Assert.NotNull(box);

        var centerX = box!.X + box.Width / 2;
        var centerY = box.Y + box.Height / 2;
        const double tolerancePx = 3;
        Assert.True(
            Math.Abs(centerX - expectedX) <= tolerancePx,
            $"expected port centered at x={expectedX}, was at x={centerX}"
        );
        Assert.True(
            Math.Abs(centerY - expectedY) <= tolerancePx,
            $"expected port centered at y={expectedY}, was at y={centerY}"
        );
    }
}
