using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class ContextMenuVisualTests : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public ContextMenuVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/placement-demo");
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(8);
    }

    private async Task OpenContextMenuOnASelectedInstance()
    {
        await PaletteEntry.Named(_page, PaletteEntry.DemoNote).ClickAsync();
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

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task RightClickOnASelectedInstanceOpensTheMenu_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await OpenContextMenuOnASelectedInstance();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task RightClickOnATwoInstanceSelectionOffersGroup_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);

        await PaletteEntry.Named(_page, PaletteEntry.DemoNote).ClickAsync();
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(1);

        await PaletteEntry.Named(_page, PaletteEntry.StressItem).ClickAsync();
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

        var selectionBox = _page.Locator(".selection-bounding-box");
        await selectionBox.ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        await Expect(_page.GetByRole(AriaRole.Menuitem, new() { Name = "Group" }))
            .ToBeVisibleAsync();
        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task RightClickOnAThreeInstanceSelectionShowsTheWholeAlignStrip_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);

        string[] placementOrder =
        [
            PaletteEntry.DemoNote,
            PaletteEntry.StressItem,
            PaletteEntry.Rectangle,
        ];
        for (var placed = 1; placed <= 3; placed++)
        {
            await PaletteEntry.Named(_page, placementOrder[placed - 1]).ClickAsync();
            await Expect(_page.Locator(".component-container")).ToHaveCountAsync(placed);
        }

        await _page.Locator(".component-container[aria-selected='true']").ClickAsync();
        await _page.Keyboard.PressAsync("Control+a");
        await Expect(_page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(3);

        await _page
            .Locator(".selection-bounding-box")
            .ClickAsync(new LocatorClickOptions { Button = MouseButton.Right });

        await Expect(
                _page
                    .GetByRole(AriaRole.Group, new() { Name = "Align and distribute" })
                    .GetByRole(AriaRole.Menuitem)
            )
            .ToHaveCountAsync(8);
        await ContentSnapshot.Verify(_page);
    }

    private async Task OpenContextMenuOnEmptyCanvas()
    {
        await PaletteEntry.Named(_page, PaletteEntry.DemoNote).ClickAsync();
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(1);

        await _page
            .Locator(".diagram-canvas")
            .ClickAsync(
                new LocatorClickOptions
                {
                    Button = MouseButton.Right,
                    Position = new Position { X = 40, Y = 40 },
                }
            );

        await Expect(_page.GetByRole(AriaRole.Menu, new() { Name = "Canvas actions" }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task RightClickOnEmptyCanvasOpensTheCanvasMenu_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Light);
        await OpenContextMenuOnEmptyCanvas();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task RightClickOnEmptyCanvasOpensTheCanvasMenu_DarkColorScheme_MatchesBaseline()
    {
        await NewPageAsync(ColorScheme.Dark);
        await OpenContextMenuOnEmptyCanvas();

        await ContentSnapshot.Verify(_page);
    }
}
