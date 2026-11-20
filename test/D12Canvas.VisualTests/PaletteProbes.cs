using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class PaletteProbes : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public PaletteProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
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

    private ILocator Rail =>
        _page.GetByRole(AriaRole.Toolbar, new() { Name = "Component palette", Exact = true });

    private ILocator RailButton(string name) =>
        Rail.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private static ILocator TooltipOf(ILocator button) =>
        button.Locator("xpath=..").Locator(".d12-tooltip");

    [Fact]
    public async Task EachRailButtonIsReachableByItsAccessibleName()
    {
        foreach (
            var name in new[]
            {
                "Edge",
                "Rectangle",
                "Sticky Note",
                "Text",
                "Image",
                PaletteEntry.DemoNote,
                PaletteEntry.StressItem,
                "Process",
                "Decision",
                "More components",
            }
        )
        {
            await Expect(RailButton(name)).ToBeVisibleAsync();
        }
    }

    [Fact]
    public async Task HoveringAButtonShowsItsDisplayNameBesideTheRail()
    {
        var button = RailButton("Sticky Note");
        var tooltip = TooltipOf(button);
        await Expect(tooltip).ToBeHiddenAsync();

        await button.HoverAsync();

        await Expect(tooltip).ToBeVisibleAsync();
        await Expect(tooltip).ToHaveTextAsync("Sticky Note");
        var railBox = (await Rail.BoundingBoxAsync())!;
        var buttonBox = (await button.BoundingBoxAsync())!;
        var tooltipBox = (await tooltip.BoundingBoxAsync())!;
        Assert.InRange(tooltipBox.X - (railBox.X + railBox.Width), 11, 13);
        Assert.InRange(
            tooltipBox.Y + tooltipBox.Height / 2 - (buttonBox.Y + buttonBox.Height / 2),
            -2,
            2
        );

        await _page.Mouse.MoveAsync(900, 650);

        await Expect(tooltip).ToBeHiddenAsync();
    }

    [Fact]
    public async Task KeyboardFocusShowsTheTooltipAndArrowKeysMoveFocusDownTheRail()
    {
        await RailButton("Edge").FocusAsync();
        await _page.Keyboard.PressAsync("ArrowDown");

        var rectangle = RailButton("Rectangle");
        await Expect(rectangle).ToBeFocusedAsync();
        await Expect(TooltipOf(rectangle)).ToBeVisibleAsync();
        await Expect(TooltipOf(rectangle)).ToHaveTextAsync("Rectangle");
        await Expect(TooltipOf(RailButton("Edge"))).ToBeHiddenAsync();

        await _page.Keyboard.PressAsync("ArrowUp");

        await Expect(RailButton("Edge")).ToBeFocusedAsync();
        await Expect(TooltipOf(RailButton("Edge"))).ToHaveTextAsync("Edge");
        await Expect(TooltipOf(RailButton("Edge"))).ToBeVisibleAsync();
    }

    private ILocator MoreButton => RailButton("More components");

    private ILocator Flyout =>
        _page.GetByRole(AriaRole.Menu, new() { Name = "More components", Exact = true });

    private ILocator FlyoutEntry(string name) =>
        Flyout.GetByRole(AriaRole.Menuitem, new() { Name = name, Exact = true });

    private async Task OpenFlyoutAsync()
    {
        await MoreButton.ClickAsync();
        await Expect(Flyout).ToBeVisibleAsync();
        await Expect(FlyoutEntry("Terminator")).ToBeFocusedAsync();
    }

    [Fact]
    public async Task OpeningTheFlyoutMovesFocusToItsFirstEntry()
    {
        await Expect(TooltipOf(MoreButton)).ToHaveTextAsync("More components (6)");

        await OpenFlyoutAsync();

        await Expect(MoreButton).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(FlyoutEntry("Terminator")).ToBeFocusedAsync();
        await Expect(Flyout.GetByRole(AriaRole.Menuitem)).ToHaveCountAsync(6);
    }

    [Fact]
    public async Task ArrowKeysMoveFocusThroughTheFlyoutEntries()
    {
        await OpenFlyoutAsync();

        await _page.Keyboard.PressAsync("ArrowDown");
        await Expect(FlyoutEntry("Data")).ToBeFocusedAsync();

        await _page.Keyboard.PressAsync("ArrowRight");
        await Expect(FlyoutEntry("Comment")).ToBeFocusedAsync();

        await _page.Keyboard.PressAsync("End");
        await Expect(FlyoutEntry("Highlight")).ToBeFocusedAsync();
        await Expect(Flyout).ToBeVisibleAsync();
    }

    [Fact]
    public async Task EscapeClosesTheFlyoutAndReturnsFocusToTheMoreButton()
    {
        await OpenFlyoutAsync();

        await _page.Keyboard.PressAsync("Escape");

        await Expect(Flyout).ToBeHiddenAsync();
        await Expect(MoreButton).ToBeFocusedAsync();
        await Expect(MoreButton).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [Fact]
    public async Task AClickOutsideClosesTheFlyoutAndReturnsFocusToTheMoreButton()
    {
        await OpenFlyoutAsync();
        var railBox = (await Rail.BoundingBoxAsync())!;

        await _page.Mouse.ClickAsync(railBox.X + 10, railBox.Y + railBox.Height + 40);

        await Expect(Flyout).ToBeHiddenAsync();
        await Expect(MoreButton).ToBeFocusedAsync();
    }

    [Fact]
    public async Task PickingAFlyoutEntryPlacesItOnTheBoardAndClosesTheFlyout()
    {
        await OpenFlyoutAsync();

        await FlyoutEntry("Data").ClickAsync();

        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".demo-shape")).ToHaveTextAsync("Data");
        await Expect(Flyout).ToBeHiddenAsync();
    }

    [Fact]
    public async Task DraggingAFlyoutEntryOntoTheBoardPlacesItThere()
    {
        await OpenFlyoutAsync();

        await FlyoutEntry("Callout")
            .DragToAsync(
                _page.Locator(".diagram-canvas"),
                new LocatorDragToOptions
                {
                    TargetPosition = new TargetPosition { X = 200, Y = 100 },
                }
            );

        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(1);
        await Expect(_page.Locator(".demo-shape")).ToHaveTextAsync("Callout");
        await Expect(Flyout).ToBeHiddenAsync();
    }

    [Fact]
    public async Task ArrowKeysOnTheRailDoNotScrollThePage()
    {
        await _page.EvaluateAsync("() => { document.body.style.minHeight = '3000px'; }");

        await RailButton("Edge").FocusAsync();
        await _page.Keyboard.PressAsync("ArrowDown");
        await _page.Keyboard.PressAsync("End");
        await _page.Keyboard.PressAsync("ArrowUp");

        await Expect(RailButton("Decision")).ToBeFocusedAsync();
        Assert.Equal(0, await _page.EvaluateAsync<double>("() => window.scrollY"));
    }
}
