using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The real keys of a keyboard port placement, from Shift+F10 through the menu's own Enter to the
// arrows and the Enter that adds the port, and the right-click on a border that adds one where it
// landed. The left shape is 240 by 120 at (120, 120); under the default snap its placement starts
// at x 180, a quarter along the top, and each arrow moves one 20px grid line.
public sealed class PortPlacementProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string ShapeId = "9d000000-0000-0000-0000-000000000001";
    private const string NeighbourId = "9d000000-0000-0000-0000-000000000002";

    protected override string ProbePagePath => "/port-placement-demo";

    protected override int ProbePageInstanceCount => 2;

    private ILocator Shape => Page.Locator($".component-container[data-d12-entity='{ShapeId}']");

    private ILocator Neighbour =>
        Page.Locator($".component-container[data-d12-entity='{NeighbourId}']");

    private ILocator Menu => Page.Locator(".d12-context-menu");

    private ILocator Provisional => Shape.Locator(".port-provisional");

    private ILocator CustomPorts => Shape.Locator(".custom-port");

    private async Task StartPlacementAsync()
    {
        await Shape.FocusAsync();
        await Expect(Shape).ToHaveAttributeAsync("aria-selected", "true");
        await Page.Keyboard.PressAsync("Shift+F10");
        await Expect(Menu.Locator(".d12-context-menu-item").First).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Menu.Locator(".d12-context-menu-item:focus .d12-context-menu-label"))
            .ToHaveTextAsync("Add port…");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Menu).ToHaveCountAsync(0);
        await Expect(Provisional).ToHaveCountAsync(1);
        await Expect(Shape).ToBeFocusedAsync();
        await SettleAsync();
    }

    private static async Task<(double X, double Y)> FractionsOfAsync(ILocator dot)
    {
        var style = await dot.GetAttributeAsync("style");
        var match = Regex.Match(style!, @"left: calc\(([\d.]+)% .*top: calc\(([\d.]+)%");
        return (
            double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture) / 100,
            double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture) / 100
        );
    }

    [Fact]
    public async Task ShiftF10AddPortTheArrowsAndEnter_AddThePortWhereTheArrowsLeftIt()
    {
        await StartPlacementAsync();
        var (startX, startY) = await FractionsOfAsync(Provisional);
        Assert.Equal(0.25, startX, 6);
        Assert.Equal(0, startY, 6);

        await Page.Keyboard.PressAsync("ArrowRight");
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Provisional).ToHaveCountAsync(0);
        await Expect(CustomPorts).ToHaveCountAsync(1);
        var (x, y) = await FractionsOfAsync(CustomPorts);
        Assert.Equal(80.0 / 240, x, 6);
        Assert.Equal(1, y, 6);
        await Expect(Shape).ToBeFocusedAsync();
        await Expect(Shape).ToHaveAttributeAsync("aria-selected", "true");
        Assert.Single(await CallsToAsync("OnEnterPressed"));
    }

    [Fact]
    public async Task EscapeEndsPlacementAddingNothingAndKeepsTheSelection()
    {
        await StartPlacementAsync();
        await Page.Keyboard.PressAsync("ArrowRight");

        await Page.Keyboard.PressAsync("Escape");

        await Expect(Provisional).ToHaveCountAsync(0);
        await Expect(CustomPorts).ToHaveCountAsync(0);
        await Expect(Shape).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task TabEndsPlacementAddingNothing()
    {
        await StartPlacementAsync();

        await Page.Keyboard.PressAsync("Tab");

        await Expect(Neighbour).ToBeFocusedAsync();
        await Expect(Provisional).ToHaveCountAsync(0);
        await Shape.ClickAsync();
        await Expect(CustomPorts).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task APressOnTheCanvasEndsPlacementAddingNothing()
    {
        await StartPlacementAsync();
        var empty = await PagePointOnCanvasAsync(400, 330);

        await Page.Mouse.ClickAsync(empty.X, empty.Y);

        await Expect(Provisional).ToHaveCountAsync(0);
        await Shape.ClickAsync();
        await Expect(CustomPorts).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task DeleteDuringPlacementLeavesTheShape()
    {
        await StartPlacementAsync();

        await Page.Keyboard.PressAsync("Delete");
        await SettleAsync();

        await Expect(Shape).ToHaveCountAsync(1);
        await Expect(Provisional).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Enter");
        await Expect(CustomPorts).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ARightClickOnTheTopSidesResizeSpan_OffersAddPortHereAndAddsThePortThere()
    {
        await Shape.ClickAsync();
        var span = Shape.Locator(".resize-span-top").First;
        await Expect(span).ToHaveCountAsync(1);
        var box = await Shape.BoundingBoxAsync();
        Assert.NotNull(box);

        await Page.Mouse.ClickAsync(
            box!.X + box.Width / 4,
            box.Y,
            new() { Button = MouseButton.Right }
        );
        await Expect(Menu).ToBeVisibleAsync();
        await Menu.GetByText("Add port here").ClickAsync();

        await Expect(CustomPorts).ToHaveCountAsync(1);
        var (x, y) = await FractionsOfAsync(CustomPorts);
        Assert.InRange(x, 0.24, 0.26);
        Assert.Equal(0, y, 6);
    }
}
