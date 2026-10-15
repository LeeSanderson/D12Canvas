using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The listener reads the rendered lock: a real primary press, drag or marquee passes a locked
// shape by, a real right-click reaches it, and Ctrl+Shift+L marks the selection locked.
public sealed class LockingProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string FreeId = "10c00000-0000-0000-0000-000000000001";
    private const string LockedId = "10c00000-0000-0000-0000-000000000002";

    protected override string ProbePagePath => "/locking-demo";

    protected override int ProbePageInstanceCount => 2;

    private ILocator Selected => Page.Locator(".component-container[aria-selected='true']");

    private ILocator Menu => Page.Locator(".d12-context-menu");

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

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
    public async Task APrimaryClickOrDragOnALockedShape_NeitherSelectsNorMovesIt()
    {
        var before = await InlinePositionAsync(Instance(LockedId));

        await Instance(LockedId).ClickAsync();
        await SettleAsync();
        await Expect(Selected).ToHaveCountAsync(0);

        var start = await CentreOfAsync(Instance(LockedId));
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X + 120, start.Y + 150, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
        await SettleAsync();

        Assert.Equal(before, await InlinePositionAsync(Instance(LockedId)));
        await Expect(Selected).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task AMarqueeOverBothShapes_SelectsOnlyTheFreeOne()
    {
        var from = await PagePointOnCanvasAsync(20, 20);
        var to = await PagePointOnCanvasAsync(520, 260);
        await Page.Mouse.MoveAsync(from.X, from.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(to.X, to.Y, new() { Steps = 8 });
        await Page.Mouse.UpAsync();
        await SettleAsync();

        await Expect(Selected).ToHaveCountAsync(1);
        await Expect(Instance(FreeId)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task ARightClickOnALockedShape_SelectsItAndItsUnlockRowFreesIt()
    {
        await Instance(LockedId).ClickAsync(new() { Button = MouseButton.Right });

        await Expect(Menu).ToBeVisibleAsync();
        await Expect(Instance(LockedId)).ToHaveAttributeAsync("aria-selected", "true");

        await Menu.GetByRole(AriaRole.Menuitem, new() { Name = "Unlock", Exact = true })
            .ClickAsync();
        await Expect(Instance(LockedId)).Not.ToHaveAttributeAsync("data-d12-locked", "true");

        await Instance(FreeId).ClickAsync();
        await Instance(LockedId).ClickAsync();
        await Expect(Instance(LockedId)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task CtrlShiftL_LocksTheSelectionSoAClickNoLongerSelectsIt()
    {
        await Instance(FreeId).ClickAsync();
        await Expect(Instance(FreeId)).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.PressAsync("Control+Shift+L");
        await Expect(Instance(FreeId)).ToHaveAttributeAsync("data-d12-locked", "true");

        var empty = await EmptyCanvasPointAsync();
        await Page.Mouse.ClickAsync(empty.X, empty.Y);
        await Expect(Selected).ToHaveCountAsync(0);

        await Instance(FreeId).ClickAsync();
        await SettleAsync();
        await Expect(Selected).ToHaveCountAsync(0);
    }
}
