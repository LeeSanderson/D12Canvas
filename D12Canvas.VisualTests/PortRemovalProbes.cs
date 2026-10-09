using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The real keys and the real right-click that remove a custom port. The left shape has one custom
// port a quarter down its right side, with an edge pinned to it running to the neighbour. Port
// picking's Space cycle starts at auto, then Top, Right, Bottom, Left, then the custom port.
public sealed class PortRemovalProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string ShapeId = "9e000000-0000-0000-0000-000000000001";
    private const string CustomPortId = "9e000000-0000-0000-0000-0000000000c1";
    private const int SpacesToCustomPort = 5;

    protected override string ProbePagePath => "/port-removal-demo";

    protected override int ProbePageInstanceCount => 2;

    private ILocator Shape => Page.Locator($".component-container[data-d12-entity='{ShapeId}']");

    private ILocator Menu => Page.Locator(".d12-context-menu");

    private ILocator CustomPorts => Shape.Locator(".custom-port");

    private ILocator EdgeLines => Page.Locator(".edge-line");

    private async Task PickAsync(int spaces)
    {
        await Shape.FocusAsync();
        await Expect(Shape).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(CustomPorts).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Enter");
        for (var i = 0; i < spaces; i++)
        {
            await Page.Keyboard.PressAsync("Space");
        }

        await SettleAsync();
    }

    [Fact]
    public async Task EnterSpaceToTheCustomPortAndDelete_RemoveThePortAndKeepTheShapeAndEdge()
    {
        await PickAsync(SpacesToCustomPort);
        await Expect(CustomPorts.First).ToHaveClassAsync(new Regex("port-focused"));

        await Page.Keyboard.PressAsync("Delete");

        await Expect(CustomPorts).ToHaveCountAsync(0);
        await Expect(Shape).ToHaveCountAsync(1);
        await Expect(EdgeLines).ToHaveCountAsync(1);
        await Expect(Shape).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Page.Keyboard.PressAsync("Control+z");
        await Expect(CustomPorts).ToHaveCountAsync(1);
        await Expect(EdgeLines).ToHaveCountAsync(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task DeleteWhilePickingAutoOrTop_LeavesTheBoardAsItWas(int spaces)
    {
        await PickAsync(spaces);

        await Page.Keyboard.PressAsync("Delete");
        await Page.Keyboard.PressAsync("Backspace");
        await SettleAsync();

        await Expect(Shape).ToHaveCountAsync(1);
        await Expect(CustomPorts).ToHaveCountAsync(1);
        await Expect(EdgeLines).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ARightClickOnTheCustomPortsSpan_OffersRemovePortAndRemovesIt()
    {
        await Shape.ClickAsync();
        var span = Shape.Locator($".port-span[data-d12-part='{CustomPortId}']");
        await Expect(span).ToHaveCountAsync(1);
        var (x, y) = await CentreOfAsync(span);

        await Page.Mouse.ClickAsync(x, y, new() { Button = MouseButton.Right });
        await Expect(Menu).ToBeVisibleAsync();
        await Expect(Menu.GetByText("Add port here")).ToHaveCountAsync(0);
        await Menu.GetByText("Remove port").ClickAsync();

        await Expect(CustomPorts).ToHaveCountAsync(0);
        await Expect(Shape).ToHaveCountAsync(1);
        await Expect(EdgeLines).ToHaveCountAsync(1);
    }
}
