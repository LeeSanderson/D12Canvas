using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The Palette rail driven by real input on the placement demo: each button is reachable by its
// accessible name inside the "Component palette" toolbar, and its library tooltip shows the
// type's name on hover and on keyboard focus. Hover states flake in screenshots, so the tooltip
// has no baseline.
public sealed class PaletteProbes : IAsyncLifetime
{
    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
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
        await Expect(_page.Locator(".d12-palette-entry")).ToHaveCountAsync(6);
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
}
