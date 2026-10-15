using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Alt reaches a drag through the real keyboard, both held from the press and pressed or released
// with the pointer still, which bUnit's direct calls never exercise.
public sealed class CloneDragProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string LeftId = "e0000000-0000-0000-0000-000000000001";
    private const string RightId = "e0000000-0000-0000-0000-000000000002";

    protected override string ProbePagePath => "/clone-drag-demo";

    protected override int ProbePageInstanceCount => 2;

    private ILocator Instances => Page.Locator(".component-container");

    private ILocator Selected => Page.Locator(".component-container[aria-selected='true']");

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator Copies =>
        Page.Locator(
            $".component-container:not([data-d12-entity='{LeftId}']):not([data-d12-entity='{RightId}'])"
        );

    private static async Task<(double Left, double Top)> InlinePositionAsync(ILocator instance)
    {
        var style = await instance.GetAttributeAsync("style");
        var match = Regex.Match(style!, @"left: (-?[\d.]+)px; top: (-?[\d.]+)px");
        return (
            double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
        );
    }

    private async Task SelectBothAsync()
    {
        await Instance(LeftId).ClickAsync();
        await Instance(RightId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(Selected).ToHaveCountAsync(2);
    }

    private async Task<(float X, float Y)> PressLeftAndDragDownAsync()
    {
        var start = await CentreOfAsync(Instance(LeftId));
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        var end = (start.X, start.Y + 200);
        await Page.Mouse.MoveAsync(end.X, end.Item2, new() { Steps = 8 });
        await SettleAsync();
        return end;
    }

    [Fact]
    public async Task AltHeldFromThePress_DropsCopiesWithTheirEdgeAndLeavesTheOriginals()
    {
        await SelectBothAsync();
        var leftBefore = await InlinePositionAsync(Instance(LeftId));
        var rightBefore = await InlinePositionAsync(Instance(RightId));

        await Page.Keyboard.DownAsync("Alt");
        await PressLeftAndDragDownAsync();
        await Page.Mouse.UpAsync();
        await Page.Keyboard.UpAsync("Alt");

        await Expect(Instances).ToHaveCountAsync(4);
        await Expect(Page.Locator(".edge-line")).ToHaveCountAsync(2);
        Assert.Equal(leftBefore, await InlinePositionAsync(Instance(LeftId)));
        Assert.Equal(rightBefore, await InlinePositionAsync(Instance(RightId)));
        await Expect(Selected).ToHaveCountAsync(2);
        await Expect(Copies.And(Selected)).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task PressingAltWithThePointerStill_SwapsTheOriginalForACopyAndReleasingItSwapsBack()
    {
        await Instance(LeftId).ClickAsync();
        var before = await InlinePositionAsync(Instance(LeftId));
        await PressLeftAndDragDownAsync();
        var moved = await InlinePositionAsync(Instance(LeftId));
        Assert.True(moved.Top > before.Top + 100, $"the original did not move: {moved}");

        await Page.Keyboard.DownAsync("Alt");
        await Expect(Instances).ToHaveCountAsync(3);
        Assert.Equal(before, await InlinePositionAsync(Instance(LeftId)));
        Assert.Equal(moved, await InlinePositionAsync(Copies));

        await Page.Keyboard.UpAsync("Alt");
        await Expect(Instances).ToHaveCountAsync(2);
        Assert.Equal(moved, await InlinePositionAsync(Instance(LeftId)));

        await Page.Mouse.UpAsync();
        await Expect(Instances).ToHaveCountAsync(2);
    }

    [Fact]
    public async Task CtrlDAfterACloneDrag_RepeatsTheDragsOffset()
    {
        await Instance(LeftId).ClickAsync();
        var source = await InlinePositionAsync(Instance(LeftId));
        await Page.Keyboard.DownAsync("Alt");
        await PressLeftAndDragDownAsync();
        await Page.Mouse.UpAsync();
        await Page.Keyboard.UpAsync("Alt");
        await Expect(Instances).ToHaveCountAsync(3);
        var copy = await InlinePositionAsync(Selected);

        await Page.Keyboard.PressAsync("Control+d");

        await Expect(Instances).ToHaveCountAsync(4);
        Assert.Equal(
            (2 * copy.Left - source.Left, 2 * copy.Top - source.Top),
            await InlinePositionAsync(Selected)
        );
    }

    [Fact]
    public async Task EscapeMidClone_LeavesTheBoardAndTheSelectionAsTheyWere()
    {
        await SelectBothAsync();
        var before = await InlinePositionAsync(Instance(LeftId));
        await Page.Keyboard.DownAsync("Alt");
        await PressLeftAndDragDownAsync();
        await Expect(Instances).ToHaveCountAsync(4);

        await Page.Keyboard.PressAsync("Escape");
        await Page.Mouse.UpAsync();
        await Page.Keyboard.UpAsync("Alt");

        await Expect(Instances).ToHaveCountAsync(2);
        await Expect(Selected).ToHaveCountAsync(2);
        Assert.Equal(before, await InlinePositionAsync(Instance(LeftId)));
    }
}
