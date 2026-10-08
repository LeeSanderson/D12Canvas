using System.Text.Json;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A modifier changed while a drag holds the pointer still reaches C# as one move from where the
// pointer last was, carrying the new modifier state.
public sealed class LiveModifierProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private async Task<JsonElement> StartDraggingTheRectangleAsync()
    {
        var start = await CentreOfAsync(
            Page.Locator(".component-container[aria-label='Rectangle']").First
        );
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X + 60, start.Y + 10, new() { Steps = 4 });
        await SettleAsync();
        var lastMove = (await CallsToAsync("OnPointerMoved"))[^1][0];
        await ClearCallsAsync();
        return lastMove;
    }

    private static void AssertSamePosition(JsonElement expected, JsonElement actual)
    {
        Assert.Equal(expected.GetProperty("x").GetDouble(), actual.GetProperty("x").GetDouble());
        Assert.Equal(expected.GetProperty("y").GetDouble(), actual.GetProperty("y").GetDouble());
    }

    [Fact]
    public async Task PressingShiftWithThePointerStill_ResendsTheLastPositionOnceWithShift()
    {
        var lastMove = await StartDraggingTheRectangleAsync();

        await Page.Keyboard.DownAsync("Shift");
        await SettleAsync();

        var resent = Assert.Single(await CallsToAsync("OnPointerMoved"))[0];
        AssertSamePosition(lastMove, resent);
        Assert.True(resent.GetProperty("shiftKey").GetBoolean());

        await Page.Keyboard.UpAsync("Shift");
        await Page.Mouse.UpAsync();
    }

    [Fact]
    public async Task AnAutoRepeatedKeydown_SendsNothingAndTheKeyupSendsOneMove()
    {
        await StartDraggingTheRectangleAsync();
        await Page.Keyboard.DownAsync("Shift");
        await SettleAsync();
        await ClearCallsAsync();

        await Page.Keyboard.DownAsync("Shift");
        await SettleAsync();
        Assert.Empty(await CallsToAsync("OnPointerMoved"));

        await Page.Keyboard.UpAsync("Shift");
        await SettleAsync();
        var released = Assert.Single(await CallsToAsync("OnPointerMoved"))[0];
        Assert.False(released.GetProperty("shiftKey").GetBoolean());

        await Page.Mouse.UpAsync();
    }

    [Fact]
    public async Task PressingCtrlWithThePointerStill_ResendsTheLastPositionOnceWithCtrl()
    {
        var lastMove = await StartDraggingTheRectangleAsync();

        await Page.Keyboard.DownAsync("Control");
        await SettleAsync();

        var resent = Assert.Single(await CallsToAsync("OnPointerMoved"))[0];
        AssertSamePosition(lastMove, resent);
        Assert.True(resent.GetProperty("ctrlKey").GetBoolean());

        await Page.Keyboard.UpAsync("Control");
        await Page.Mouse.UpAsync();
    }

    [Fact]
    public async Task AModifierChangeBeforeTheDragThreshold_SendsNoMove()
    {
        var start = await EmptyCanvasPointAsync();
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await SettleAsync();

        await Page.Keyboard.DownAsync("Shift");
        await Page.Keyboard.UpAsync("Shift");
        await SettleAsync();

        Assert.Empty(await CallsToAsync("OnPointerMoved"));
        await Page.Mouse.UpAsync();
    }

    [Fact]
    public async Task AMoveResentForAModifierCarriesZeroVelocity()
    {
        await StartDraggingTheRectangleAsync();

        await Page.Keyboard.DownAsync("Control");
        await SettleAsync();

        var resent = Assert.Single(await CallsToAsync("OnPointerMoved"))[0];
        Assert.Equal(0, resent.GetProperty("velocity").GetDouble());

        await Page.Keyboard.UpAsync("Control");
        await Page.Mouse.UpAsync();
    }

    [Fact]
    public async Task PressingAltMidResize_GrowsAboutTheCentreAndReleasingItPutsTheFarEdgeBack()
    {
        var rectangle = Page.Locator(".component-container[aria-label='Rectangle']").First;
        var centre = await CentreOfAsync(rectangle);
        await Page.Mouse.ClickAsync(centre.X, centre.Y);
        var handle = rectangle.Locator(".resize-handle.right");
        await Expect(handle).ToBeVisibleAsync();
        var before = await BoxOfAsync(rectangle);
        var start = await CentreOfAsync(handle);
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X + 60, start.Y, new() { Steps = 4 });
        await SettleAsync();
        var plain = await BoxOfAsync(rectangle);

        await Page.Keyboard.DownAsync("Alt");
        await SettleAsync();
        var centred = await BoxOfAsync(rectangle);
        Assert.Equal(before.X + before.Width / 2, centred.X + centred.Width / 2, 0.5);
        Assert.True(
            centred.X < plain.X - 20,
            $"left edge {centred.X} did not move out from {plain.X}"
        );

        await Page.Keyboard.UpAsync("Alt");
        await SettleAsync();
        var released = await BoxOfAsync(rectangle);
        Assert.Equal(plain.X, released.X, 0.5);
        Assert.Equal(plain.Width, released.Width, 0.5);

        await Page.Mouse.UpAsync();
    }

    private static async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        return box!;
    }
}
