using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Screenshot-diff baselines for the four layering commands (ticket 60/ADR 0008): two overlapping
// instances before a layering command, and after Bring to Front moves the originally-underneath
// one above the other. Any later ticket that renders a new visual state on canvas should add a
// case here alongside its own.
public sealed class ZIndexLayeringVisualTests : IAsyncLifetime
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
    public ZIndexLayeringVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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

    // Click-to-adds the first palette entry and selects it while it's still the board's only
    // instance - avoiding any risk of a click landing on the second instance instead once it's
    // placed on top (ticket 60/ADR 0008's new-on-top default) - then click-to-adds a second,
    // different entry so the two overlap (the cascading +20,+20 offset from ticket 28).
    private async Task PlaceTwoOverlappingInstancesWithTheFirstSelected()
    {
        var entries = _page.Locator(".d12-palette-entry-button");
        await entries.Nth(0).ClickAsync();
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(1);

        await _page.Locator(".component-container").ClickAsync();
        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(1);

        await entries.Nth(1).ClickAsync();
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task SecondPlacedInstanceOverlapsAndRendersAboveTheFirstByDefault_MatchesBaseline()
    {
        await PlaceTwoOverlappingInstancesWithTheFirstSelected();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task BringToFrontMovesTheFirstPlacedInstanceAboveTheSecond_MatchesBaseline()
    {
        await PlaceTwoOverlappingInstancesWithTheFirstSelected();

        await _page.Keyboard.PressAsync("Control+Shift+]");

        // Confirms the shortcut actually reordered the stack (not just that some screenshot was
        // taken) - the selected instance's own z-index must now exceed the other's.
        var selectedZIndex = await ZIndexOf(
            _page.Locator(".component-container[aria-selected='true']")
        );
        var otherZIndex = await ZIndexOf(
            _page.Locator(".component-container:not([aria-selected='true'])")
        );
        Assert.True(selectedZIndex > otherZIndex);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    private static async Task<int> ZIndexOf(ILocator container)
    {
        var style = await container.GetAttributeAsync("style");
        Assert.NotNull(style);
        var marker = "z-index: ";
        var start = style!.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var end = style.IndexOf(';', start);
        return int.Parse(style[start..end]);
    }
}
