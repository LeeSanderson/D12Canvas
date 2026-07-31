using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A persisted Group's tab stop: one invisible, keyboard-only focusable
// element (never visible on its own) that selects the whole group when it receives DOM focus -
// the same .selection-bounding-box overlay every other multi-selection path already draws, but
// reached here purely via focus, with no click/marquee/keyboard-shortcut involved at all.
public sealed class GroupTabStopVisualTests : IAsyncLifetime
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

    public GroupTabStopVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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

    // Click-to-add's cascading offset (20px) leaves two placed instances heavily overlapping, so
    // a direct click on one can land on the other's (later, higher-ZIndex) container instead - a
    // marquee drag selects both unambiguously by bounding rect, the same technique
    // MultiSelectionMoveResizeVisualTests.SelectBothInstances already uses for this exact reason.
    private async Task SelectBothInstancesViaMarquee()
    {
        var entries = _page.Locator(".d12-palette-entry-button");
        await entries.Nth(0).ClickAsync();
        await entries.Nth(1).ClickAsync();
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);

        var boxes = await _page.Locator(".component-container").AllAsync();
        var first = await boxes[0].BoundingBoxAsync();
        var second = await boxes[1].BoundingBoxAsync();
        Assert.NotNull(first);
        Assert.NotNull(second);

        var from = (
            (float)Math.Min(first!.X, second!.X) - 20,
            (float)Math.Min(first.Y, second.Y) - 20
        );
        var to = (
            (float)Math.Max(first.X + first.Width, second.X + second.Width) + 20,
            (float)Math.Max(first.Y + first.Height, second.Y + second.Height) + 20
        );

        await _page.Mouse.MoveAsync(from.Item1, from.Item2);
        await _page.Keyboard.DownAsync("Shift");
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(to.Item1, to.Item2);
        await _page.Mouse.UpAsync();
        await _page.Keyboard.UpAsync("Shift");

        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);
    }

    [Fact]
    public async Task FocusingAPersistedGroupsTabStop_ShowsTheBoundingBoxOverlay_MatchesBaseline()
    {
        await SelectBothInstancesViaMarquee();
        await _page.Keyboard.PressAsync("Control+g");
        await Expect(_page.Locator(".selection-bounding-box")).ToBeVisibleAsync();

        // Ctrl+G itself already moves DOM focus to the new group's own tab stop - clicking empty
        // canvas both clears the selection (the bounding box must disappear) and blurs that stop,
        // so the FocusAsync below is a genuine focus transition rather than a same-element no-op.
        await _page
            .Locator(".diagram-canvas")
            .ClickAsync(
                new()
                {
                    Position = new Position { X = 900, Y = 500 },
                }
            );
        await Expect(_page.Locator(".selection-bounding-box")).Not.ToBeVisibleAsync();

        await _page.Locator(".group-tab-stop").FocusAsync();

        await Expect(_page.Locator(".selection-bounding-box")).ToBeVisibleAsync();
        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }
}
