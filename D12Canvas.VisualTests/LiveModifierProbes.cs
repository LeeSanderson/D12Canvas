using System.Text.Json;

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
}
