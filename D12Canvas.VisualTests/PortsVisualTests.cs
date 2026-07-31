using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class PortsVisualTests : IAsyncLifetime
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
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/placement-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(6);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task PortsVisibleOnHover_MatchesBaseline()
    {
        await _page.Locator(".d12-palette-entry-button").First.ClickAsync();
        var target = _page.Locator(".component-container");

        // Hover alone (no click/select) is enough to reveal ports - proves they're an
        // independent affordance from the resize handles, which need selection.
        await target.HoverAsync();

        await Expect(_page.Locator(".port").First).ToHaveCSSAsync("opacity", "1");
        await Expect(target).Not.ToHaveAttributeAsync("aria-selected", "true");

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    // Not a screenshot baseline (no Verify call) - this is a geometric assertion, checked with
    // real browser-measured positions rather than the CSS-percentage reasoning in
    // ComponentContainer.razor's comments. Ports have layout/a bounding box regardless of their
    // own opacity, so no hover is needed to measure them; that keeps this test from depending on
    // which of two possibly-overlapping instances currently happens to sit on top.
    [Fact]
    public async Task PortsSitAtEachInstancesOwnBorderCenters()
    {
        // Rectangle (160x100) and Image (240x180) have different DefaultSizes - placing both
        // and checking each one's ports against its *own* measured box proves the positioning is
        // a genuine fraction of that instance's Bounds, not a fixed offset that only happens to
        // look right for one particular size.
        await _page.Locator("button[aria-label='Rectangle']").ClickAsync();
        await _page.Locator("button[aria-label='Image']").ClickAsync();

        var containers = _page.Locator(".component-container");
        await Expect(containers).ToHaveCountAsync(2);

        for (var i = 0; i < 2; i++)
        {
            var instance = containers.Nth(i);
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
