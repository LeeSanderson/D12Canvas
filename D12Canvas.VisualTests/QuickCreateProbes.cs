using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A real click on a port, the real Ctrl+Arrow chord and the focus the browser lands on, none of
// which bUnit's direct calls exercise. At the default zoom the copy sits two grid cells, 40px,
// past the pressed side.
public sealed class QuickCreateProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string RectangleId = "9c000000-0000-0000-0000-000000000001";
    private const string TextId = "9c000000-0000-0000-0000-000000000002";

    protected override string ProbePagePath => "/quick-create-demo";

    protected override int ProbePageInstanceCount => 2;

    private ILocator Instances => Page.Locator(".component-container");

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator Copies =>
        Page.Locator(
            $".component-container:not([data-d12-entity='{RectangleId}']):not([data-d12-entity='{TextId}'])"
        );

    private ILocator Editor => Page.Locator("textarea.d12-text-editor");

    private static async Task<(double Left, double Top)> InlinePositionAsync(ILocator instance)
    {
        var style = await instance.GetAttributeAsync("style");
        var match = Regex.Match(style!, @"left: (-?[\d.]+)px; top: (-?[\d.]+)px");
        return (
            double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
        );
    }

    [Fact]
    public async Task ClickingASelectedShapesRightPort_MakesAConnectedCopyThatTakesSelectionAndFocus()
    {
        var port = await PortGestures.SelectAndAimAtPortAsync(Instance(RectangleId), "Right");

        await Page.Mouse.ClickAsync(port.X, port.Y);

        await Expect(Instances).ToHaveCountAsync(3);
        await Expect(Page.Locator(".edge-line")).ToHaveCountAsync(1);
        Assert.Equal((220, 80), await InlinePositionAsync(Copies));
        await Expect(Copies).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Instance(RectangleId)).Not.ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Copies).ToBeFocusedAsync();
    }

    [Fact]
    public async Task CtrlArrowRightThenCtrlArrowDown_ChainsFromEachNewShape()
    {
        await Instance(RectangleId).FocusAsync();

        await Page.Keyboard.PressAsync("Control+ArrowRight");
        await Expect(Instances).ToHaveCountAsync(3);
        await Expect(Copies).ToBeFocusedAsync();
        Assert.Equal((220, 80), await InlinePositionAsync(Copies));

        await Page.Keyboard.PressAsync("Control+ArrowDown");
        await Expect(Instances).ToHaveCountAsync(4);
        await Expect(Page.Locator(".edge-line")).ToHaveCountAsync(2);
        var below = Page.Locator(".component-container[aria-selected='true']");
        await Expect(below).ToBeFocusedAsync();
        Assert.Equal((220, 200), await InlinePositionAsync(below));
    }

    [Fact]
    public async Task APortPressDraggedPastTheThreshold_DrawsAConnectorAndCreatesNoShape()
    {
        var port = await PortGestures.SelectAndAimAtPortAsync(Instance(RectangleId), "Right");
        var empty = await PagePointOnCanvasAsync(500, 420);

        await Page.Mouse.MoveAsync(port.X, port.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(empty.X, empty.Y, new() { Steps = 6 });
        await Page.Mouse.UpAsync();

        await Expect(Page.Locator(".edge-line")).ToHaveCountAsync(1);
        await Expect(Instances).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task EscapeDuringAPortPress_CreatesNothingAtRelease()
    {
        var port = await PortGestures.SelectAndAimAtPortAsync(Instance(RectangleId), "Right");

        await Page.Mouse.MoveAsync(port.X, port.Y);
        await Page.Mouse.DownAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Page.Mouse.UpAsync();
        await SettleAsync();

        await Expect(Instances).ToHaveCountAsync(2);
        await Expect(Page.Locator(".edge-line")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task CtrlArrowDownOnAText_OpensTheCopyForTypingWithItsLabelSelected()
    {
        await Instance(TextId).FocusAsync();

        await Page.Keyboard.PressAsync("Control+ArrowDown");

        await Expect(Instances).ToHaveCountAsync(3);
        await Expect(Editor).ToBeFocusedAsync();
        await Expect(Editor).ToHaveValueAsync("Login");
        await Page.Keyboard.TypeAsync("Dashboard");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Editor).ToHaveCountAsync(0);
        await Expect(Copies.Locator("p.d12-text")).ToHaveTextAsync("Dashboard");
        await Expect(Instance(TextId).Locator("p.d12-text")).ToHaveTextAsync("Login");
        await Expect(Copies).ToBeFocusedAsync();
    }

    [Fact]
    public async Task EscapeOnAnEmptiedQuickCreatedText_RemovesItAndFocusesTheSource()
    {
        await Instance(TextId).FocusAsync();
        await Page.Keyboard.PressAsync("Control+ArrowRight");
        await Expect(Editor).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Delete");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Instances).ToHaveCountAsync(2);
        await Expect(Page.Locator(".edge-line")).ToHaveCountAsync(0);
        await Expect(Instance(TextId)).ToBeFocusedAsync();
        await Expect(Instance(TextId)).ToHaveAttributeAsync("aria-selected", "true");
    }
}
