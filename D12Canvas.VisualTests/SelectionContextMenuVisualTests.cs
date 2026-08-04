using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class SelectionContextMenuVisualTests : IAsyncLifetime
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
    public SelectionContextMenuVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private async Task NewPageAsync(ColorScheme colorScheme)
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/placement-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(6);
    }

    private async Task OpenContextMenuOnASelectedInstance()
    {
        await _page.Locator(".d12-palette-entry-button").First.ClickAsync();
        var instance = _page.Locator(".component-container");
        await instance.ClickAsync();
        await Expect(instance).ToHaveAttributeAsync("aria-selected", "true");

        await instance.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        await Expect(_page.Locator(".d12-context-menu")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task RightClickOnASelectedInstanceOpensTheMenu_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await OpenContextMenuOnASelectedInstance();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task RightClickOnASelectedInstanceOpensTheMenu_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await OpenContextMenuOnASelectedInstance();

        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    // Places two cascaded, overlapping instances (the +20,+20 click-to-add offset doesn't fully
    // clear a default-sized instance) and selects both. ClickToAdd selects whichever instance it
    // just placed, so after both exist only the second (now-on-top) one is still selected - the
    // first is shift-clicked back in at a point 5,5 inside its own top-left corner, a spot the
    // second instance's own +20,+20-offset box can never reach regardless of either instance's
    // actual size, rather than trying to reach it anywhere the second, topmost instance covers it.
    [Fact]
    public async Task RightClickOnATwoInstanceSelectionOffersGroup_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);

        var entries = _page.Locator(".d12-palette-entry-button");
        await entries.Nth(0).ClickAsync();
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(1);

        await entries.Nth(1).ClickAsync();
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);

        var firstInstance = _page.Locator(".component-container").First;
        await firstInstance.ClickAsync(
            new LocatorClickOptions
            {
                Modifiers = [KeyboardModifier.Shift],
                Position = new Position { X = 5, Y = 5 },
            }
        );
        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(2);

        // The second, now-on-top instance is the only one reachable by a real click at its own
        // default (center) position.
        var topInstance = _page.Locator(".component-container").Nth(1);
        await topInstance.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        await Expect(_page.GetByRole(AriaRole.Menuitem, new() { Name = "Group" }))
            .ToBeVisibleAsync();
        await Verify(_page).PageScreenshotOptions(ScreenshotOptions);
    }

    [Fact]
    public async Task RightClickOnEmptyCanvasOpensNoMenu()
    {
        await NewPageAsync(ColorScheme.Light);

        await _page
            .Locator(".diagram-canvas")
            .ClickAsync(
                new LocatorClickOptions
                {
                    Button = MouseButton.Right,
                    Position = new Position { X = 10, Y = 10 },
                }
            );

        await Expect(_page.Locator(".d12-context-menu")).ToHaveCountAsync(0);
    }
}
