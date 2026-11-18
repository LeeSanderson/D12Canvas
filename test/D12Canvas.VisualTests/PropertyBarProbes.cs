using Microsoft.Playwright;
using Xunit;
using static D12Canvas.VisualTests.PropertyBarVisualTests;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The property bar driven by real input on the light pane of the property bar demo: absent through
// a drag and while a menu is open, chrome hidden only while the pointer is on it, Ctrl+Enter in,
// arrows across and Escape back to the shape, a recolour that shows on the edge at once, a choice
// chip's flyout taking focus and handing it back, the colour flyout's grid, swatch tooltip and
// "Custom…", a flyout opening above its chip when the selection is low on the board, and the bar
// kept inside a narrow container.
public sealed class PropertyBarProbes : IAsyncLifetime
{
    private const string Theme = "light";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public PropertyBarProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
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
                ViewportSize = new ViewportSize { Width = 1200, Height = 640 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/property-bar-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(6);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Bar => Pane(_page, Theme).Locator(".d12-property-bar");

    private ILocator Blue => Shape(_page, Theme, BlueRectangleId);

    private Task<string> FocusedLabelAsync() =>
        _page.EvaluateAsync<string>(
            "() => document.activeElement?.getAttribute('aria-label') ?? ''"
        );

    private ILocator RoutingChip => Bar.Locator(".d12-property-bar-chip[aria-label^='Routing: ']");

    private ILocator ChipFlyout => Bar.Locator(".d12-flyout");

    private ILocator ColourChip => Bar.Locator(".d12-property-bar-chip[aria-label^='Colour: ']");

    private ILocator MenuItemRadio(string name) =>
        ChipFlyout.GetByRole(AriaRole.Menuitemradio, new() { Name = name, Exact = true });

    private ILocator SwatchTooltip(string name) =>
        ChipFlyout.Locator($".d12-tooltip-anchor:has([aria-label='{name}']) .d12-tooltip");

    private async Task OpenTheColourFlyoutFromTheKeyboardAsync()
    {
        await SelectEdgeAsync(_page, Theme);
        await _page.Keyboard.PressAsync("Control+Enter");
        for (var step = 0; step < 3; step++)
        {
            await _page.Keyboard.PressAsync("ArrowRight");
        }

        Assert.Equal("Colour: #E5246B", await FocusedLabelAsync());
        await _page.Keyboard.PressAsync("Enter");
        await Expect(MenuItemRadio("Black")).ToBeFocusedAsync();
    }

    private async Task OpenTheRoutingFlyoutAsync()
    {
        await SelectEdgeAsync(_page, Theme);
        await RoutingChip.ClickAsync();
        await Expect(ChipFlyout).ToBeVisibleAsync();
        await Expect(ChipFlyout.GetByRole(AriaRole.Menuitemradio).First).ToBeFocusedAsync();
    }

    [Fact]
    public async Task TheBarIsAbsentThroughADragAndReturnsAfterTheRelease()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);
        var box = (await Blue.BoundingBoxAsync())!;
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;

        await _page.Mouse.MoveAsync(x, y);
        await _page.Mouse.DownAsync();
        await _page.Mouse.MoveAsync(x + 30, y + 30, new() { Steps = 3 });
        await Expect(Bar).ToHaveCountAsync(0);

        await _page.Mouse.UpAsync();
        await Expect(Bar).ToBeVisibleAsync();
    }

    [Fact]
    public async Task TheBarIsAbsentWhileAMenuIsOpenAndReturnsWhenItCloses()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);

        await Blue.ClickAsync(new() { Button = MouseButton.Right });
        await Expect(Pane(_page, Theme).Locator(".d12-context-menu")).ToBeVisibleAsync();
        await Expect(Bar).ToHaveCountAsync(0);

        await _page.Keyboard.PressAsync("Escape");
        await Expect(Pane(_page, Theme).Locator(".d12-context-menu")).ToHaveCountAsync(0);
        await Expect(Bar).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HoveringTheBarHidesTheOutlineHandlesAndPortsAndLeavingRestoresThem()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);
        var handle = Blue.Locator(".resize-handle").First;
        await Expect(handle).ToBeVisibleAsync();

        await Bar.Locator(".d12-property-bar-cell").First.HoverAsync();

        await Expect(handle).ToBeHiddenAsync();
        await Expect(Blue.Locator(".port").First).ToBeHiddenAsync();
        Assert.Equal(
            "rgba(0, 0, 0, 0)",
            await Blue.EvaluateAsync<string>("el => getComputedStyle(el).outlineColor")
        );

        await _page.Mouse.MoveAsync(5, 5);

        await Expect(handle).ToBeVisibleAsync();
        Assert.NotEqual(
            "rgba(0, 0, 0, 0)",
            await Blue.EvaluateAsync<string>("el => getComputedStyle(el).outlineColor")
        );
    }

    [Fact]
    public async Task HoveringAnEdgesBarHidesItsHalo()
    {
        await SelectEdgeAsync(_page, Theme);
        var halo = Pane(_page, Theme).Locator(".edge-halo");

        await Bar.Locator(".d12-property-bar-cell").First.HoverAsync();
        await Expect(halo).ToBeHiddenAsync();

        await _page.Mouse.MoveAsync(5, 5);
        await Expect(halo).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CtrlEnterFocusesTheFirstGlyphArrowsRoveAndEscapeReturnsToTheShape()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);
        await Blue.FocusAsync();

        await _page.Keyboard.PressAsync("Control+Enter");
        Assert.Equal("Fill: #CFE3FF", await FocusedLabelAsync());

        await _page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("Stroke: Theme", await FocusedLabelAsync());
        await _page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("Stroke width", await FocusedLabelAsync());
        await _page.Keyboard.PressAsync("ArrowRight");
        Assert.Equal("Fill: #CFE3FF", await FocusedLabelAsync());
        await _page.Keyboard.PressAsync("ArrowLeft");
        Assert.Equal("Stroke width", await FocusedLabelAsync());

        var before = await Blue.GetAttributeAsync("style");
        await _page.Keyboard.PressAsync("Escape");

        Assert.Equal(
            BlueRectangleId,
            await _page.EvaluateAsync<string>(
                "() => document.activeElement?.getAttribute('data-d12-entity') ?? ''"
            )
        );
        await Expect(Blue).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("selected"));
        Assert.Equal(before, await Blue.GetAttributeAsync("style"));
    }

    [Fact]
    public async Task CtrlEnterAfterAClickReachesTheBarAndEscapeFocusesTheSelectedShape()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);

        await _page.Keyboard.PressAsync("Control+Enter");
        Assert.Equal("Fill: #CFE3FF", await FocusedLabelAsync());

        await _page.Keyboard.PressAsync("Escape");
        Assert.Equal(
            BlueRectangleId,
            await _page.EvaluateAsync<string>(
                "() => document.activeElement?.getAttribute('data-d12-entity') ?? ''"
            )
        );
    }

    [Fact]
    public async Task DeleteOnAColourGlyphReturnsItToTheThemeAndLeavesTheEdgeOnTheBoard()
    {
        await SelectEdgeAsync(_page, Theme);
        var line = Pane(_page, Theme).Locator(".edge-line");
        await _page.Keyboard.PressAsync("Control+Enter");
        for (var step = 0; step < 3; step++)
        {
            await _page.Keyboard.PressAsync("ArrowRight");
        }

        Assert.Equal("Colour: #E5246B", await FocusedLabelAsync());
        await _page.Keyboard.PressAsync("Delete");

        await Expect(Bar.Locator(".d12-property-bar-colour-themed")).ToHaveCountAsync(1);
        await Expect(line)
            .Not.ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("#e5246b"));
        await Expect(Pane(_page, Theme).Locator(".edge-line")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ARecolourFromTheBarShowsOnTheEdgeAtOnceAndUndoesInOneStep()
    {
        await SelectEdgeAsync(_page, Theme);
        var line = Pane(_page, Theme).Locator(".edge-line");

        await Bar.Locator("input[aria-label^='Colour: ']").FillAsync("#2ecc71");

        await Expect(line)
            .ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("#2ecc71"));
        Assert.Equal(
            "rgb(46, 204, 113)",
            await line.EvaluateAsync<string>("el => getComputedStyle(el).stroke")
        );

        await Pane(_page, Theme).Locator(".diagram-canvas").FocusAsync();
        await _page.Keyboard.PressAsync("Control+z");
        await Expect(line)
            .ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("#e5246b"));
    }

    // A wheel moves the viewport under a visible bar. The bar hides while it is being moved and
    // shows again where the selection now is once the wheel has stopped.
    [Fact]
    public async Task AWheelHidesTheBarWhileTheViewMovesAndShowsItAgainWhenItStops()
    {
        await SelectAsync(_page, Theme, BlueRectangleId);
        await Expect(Bar).ToBeVisibleAsync();
        await Bar.EvaluateAsync(
            """
            bar => {
                window.__d12BarSettled = [];
                new MutationObserver(() =>
                    window.__d12BarSettled.push(bar.classList.contains("d12-property-bar-settling"))
                ).observe(bar, { attributes: true, attributeFilter: ["class"] });
            }
            """
        );
        var before = await Bar.GetAttributeAsync("style");
        var container = (
            await Pane(_page, Theme).Locator(".diagram-container").BoundingBoxAsync()
        )!;

        await _page.Mouse.MoveAsync(container.X + 20, container.Y + container.Height - 20);
        await _page.Mouse.WheelAsync(0, 300);
        await _page.Mouse.WheelAsync(0, 300);

        await Expect(Bar)
            .Not.ToHaveClassAsync(
                new System.Text.RegularExpressions.Regex("d12-property-bar-settling")
            );
        await Expect(Bar).ToBeVisibleAsync();
        Assert.NotEqual(before, await Bar.GetAttributeAsync("style"));
        Assert.Equal(
            [true, false],
            (await _page.EvaluateAsync<bool[]>("() => window.__d12BarSettled")).Distinct()
        );
    }

    [Fact]
    public async Task AChipOpensItsFlyoutWithFocusInsideAndEscapeReturnsFocusToTheChip()
    {
        await OpenTheRoutingFlyoutAsync();

        await Expect(RoutingChip).ToHaveAttributeAsync("aria-expanded", "true");

        await _page.Keyboard.PressAsync("Escape");

        await Expect(ChipFlyout).ToHaveCountAsync(0);
        await Expect(RoutingChip).ToBeFocusedAsync();
        await Expect(RoutingChip).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Bar).ToBeVisibleAsync();
    }

    [Fact]
    public async Task AClickOutsideClosesTheChipFlyoutAndReturnsFocusToTheChip()
    {
        await OpenTheRoutingFlyoutAsync();
        var pane = (await Pane(_page, Theme).BoundingBoxAsync())!;

        await _page.Mouse.ClickAsync(pane.X + pane.Width + 8, pane.Y + 100);

        await Expect(ChipFlyout).ToHaveCountAsync(0);
        await Expect(RoutingChip).ToBeFocusedAsync();
    }

    [Fact]
    public async Task ChoosingAnOptionCommitsItClosesTheFlyoutAndUndoesInOneStep()
    {
        await OpenTheRoutingFlyoutAsync();
        await Expect(ChipFlyout.Locator("[role=menuitemradio][aria-checked=true]"))
            .ToHaveTextAsync("Orthogonal");

        await ChipFlyout.GetByRole(AriaRole.Menuitemradio, new() { Name = "Curved" }).ClickAsync();

        await Expect(ChipFlyout).ToHaveCountAsync(0);
        await Expect(RoutingChip).ToHaveAttributeAsync("aria-label", "Routing: Curved");
        await Expect(RoutingChip).ToBeFocusedAsync();

        await Pane(_page, Theme).Locator(".diagram-canvas").FocusAsync();
        await _page.Keyboard.PressAsync("Control+z");
        await Expect(RoutingChip).ToHaveAttributeAsync("aria-label", "Routing: Orthogonal");
    }

    [Fact]
    public async Task TheKeyboardOpensAChipMovesThroughItsOptionsAndPicksOne()
    {
        await SelectEdgeAsync(_page, Theme);
        await _page.Keyboard.PressAsync("Control+Enter");
        Assert.Equal("Routing: Orthogonal", await FocusedLabelAsync());

        await _page.Keyboard.PressAsync("Enter");
        await Expect(ChipFlyout).ToBeVisibleAsync();
        await Expect(ChipFlyout.GetByRole(AriaRole.Menuitemradio).First).ToBeFocusedAsync();

        await _page.Keyboard.PressAsync("End");
        await Expect(ChipFlyout.GetByRole(AriaRole.Menuitemradio, new() { Name = "Curved" }))
            .ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("ArrowDown");
        await Expect(ChipFlyout.GetByRole(AriaRole.Menuitemradio, new() { Name = "Straight" }))
            .ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("Enter");

        await Expect(ChipFlyout).ToHaveCountAsync(0);
        await Expect(RoutingChip).ToHaveAttributeAsync("aria-label", "Routing: Straight");
        await Expect(Bar).ToBeVisibleAsync();
    }

    [Fact]
    public async Task TheKeyboardCrossesThePresetGridByRowsAndPicksASwatch()
    {
        await OpenTheColourFlyoutFromTheKeyboardAsync();
        var line = Pane(_page, Theme).Locator(".edge-line");

        await _page.Keyboard.PressAsync("ArrowDown");
        await Expect(MenuItemRadio("Yellow")).ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("ArrowRight");
        await Expect(MenuItemRadio("Green")).ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("ArrowDown");
        await Expect(MenuItemRadio("Theme colour")).ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("ArrowUp");
        await Expect(MenuItemRadio("Pink")).ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("ArrowUp");
        await Expect(MenuItemRadio("Orange")).ToBeFocusedAsync();
        await Expect(SwatchTooltip("Orange")).ToHaveTextAsync("Orange #F08C00");
        await Expect(SwatchTooltip("Orange")).ToBeVisibleAsync();

        await _page.Keyboard.PressAsync("Enter");

        await Expect(ChipFlyout).ToHaveCountAsync(0);
        await Expect(ColourChip).ToHaveAttributeAsync("aria-label", "Colour: #F08C00");
        await Expect(ColourChip).ToBeFocusedAsync();
        await Expect(line)
            .ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("#F08C00"));

        await Pane(_page, Theme).Locator(".diagram-canvas").FocusAsync();
        await _page.Keyboard.PressAsync("Control+z");
        await Expect(ColourChip).ToHaveAttributeAsync("aria-label", "Colour: #E5246B");
    }

    [Fact]
    public async Task HoveringASwatchShowsItsNameAndHex()
    {
        await SelectEdgeAsync(_page, Theme);
        await ColourChip.ClickAsync();
        await Expect(MenuItemRadio("Black")).ToBeFocusedAsync();
        var blueTooltip = SwatchTooltip("Blue");
        await Expect(blueTooltip).ToBeHiddenAsync();

        await MenuItemRadio("Blue").HoverAsync();

        await Expect(blueTooltip).ToBeVisibleAsync();
        await Expect(blueTooltip).ToHaveTextAsync("Blue #2F80ED");
    }

    [Fact]
    public async Task CustomClosesTheFlyoutAndHandsFocusToTheNativePicker()
    {
        await OpenTheColourFlyoutFromTheKeyboardAsync();

        await _page.Keyboard.PressAsync("End");
        await Expect(ChipFlyout.GetByRole(AriaRole.Menuitem, new() { Name = "Custom…" }))
            .ToBeFocusedAsync();
        await _page.Keyboard.PressAsync("Enter");

        await Expect(ChipFlyout).ToHaveCountAsync(0);
        await Expect(Bar.Locator(".d12-property-bar-picker")).ToBeFocusedAsync();
        Assert.Equal("Colour: Custom colour", await FocusedLabelAsync());
        await Expect(ColourChip).ToHaveAttributeAsync("aria-label", "Colour: #E5246B");
    }

    // The edge runs along the bottom of the pane and is left unframed, so a flyout opened below its
    // chip would run past the container's bottom edge.
    [Fact]
    public async Task AFlyoutOnASelectionNearTheBottomOpensAboveItsChipInsideTheContainer()
    {
        var container = Pane(_page, Theme).Locator(".diagram-container");
        await SelectEdgeAsync(_page, Theme);

        await ColourChip.ClickAsync();
        await Expect(MenuItemRadio("Black")).ToBeFocusedAsync();

        var outer = (await container.BoundingBoxAsync())!;
        var flyout = (await ChipFlyout.BoundingBoxAsync())!;
        var chip = (await ColourChip.BoundingBoxAsync())!;
        Assert.True(
            flyout.Y >= outer.Y && flyout.Y + flyout.Height <= outer.Y + outer.Height,
            $"The {flyout.Height}px flyout at {flyout.Y} should sit inside the {outer.Height}px container at {outer.Y}."
        );
        Assert.True(
            flyout.Y + flyout.Height <= chip.Y,
            $"The flyout ending at {flyout.Y + flyout.Height} should open above the chip at {chip.Y}."
        );
    }

    // Chips make the bar wider than the old glyph cells, so a narrow pane checks that the existing
    // clamp still keeps the whole bar inside the container for the widest bar the demo shows.
    [Fact]
    public async Task ANarrowContainerStillHoldsTheWholeBar()
    {
        await Pane(_page, Theme).EvaluateAsync("pane => { pane.style.flex = '0 0 440px'; }");
        var container = Pane(_page, Theme).Locator(".diagram-container");

        await SelectEdgeAsync(_page, Theme);
        await AssertTheBarIsInsideAsync(container);

        await SelectAsync(_page, Theme, OrangeRectangleId);
        await AssertTheBarIsInsideAsync(container);
    }

    private async Task AssertTheBarIsInsideAsync(ILocator container)
    {
        await Expect(Bar)
            .Not.ToHaveClassAsync(
                new System.Text.RegularExpressions.Regex("d12-property-bar-settling")
            );
        var outer = (await container.BoundingBoxAsync())!;
        var bar = (await Bar.BoundingBoxAsync())!;
        Assert.True(
            bar.X >= outer.X && bar.X + bar.Width <= outer.X + outer.Width,
            $"The {bar.Width}px bar at {bar.X} should sit inside the {outer.Width}px container at {outer.X}."
        );
    }
}
