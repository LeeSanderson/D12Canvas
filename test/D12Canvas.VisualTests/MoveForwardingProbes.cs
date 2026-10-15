namespace D12Canvas.VisualTests;

// The press is a primary press on empty canvas, which the listener claims. The distances are
// chosen to sit well clear of the drag threshold on either side rather than to locate it.
public sealed class MoveForwardingProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    [Fact]
    public async Task APressThatStaysWithinTheThreshold_SendsNoMoveToCSharp()
    {
        var start = await EmptyCanvasPointAsync();
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X + 1, start.Y);
        await Page.Mouse.MoveAsync(start.X + 1, start.Y + 1);
        await Page.Mouse.MoveAsync(start.X, start.Y + 1);
        await SettleAsync();

        await SinglePressAsync();
        Assert.Empty(await CallsToAsync("OnPointerMoved"));

        await Page.Mouse.UpAsync();
        await SettleAsync();
        Assert.Single(await CallsToAsync("OnPointerReleased"));
    }

    [Fact]
    public async Task APressThatCrossesTheThreshold_SendsItsMovesToCSharp()
    {
        var start = await EmptyCanvasPointAsync();
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X + 30, start.Y + 20, new() { Steps = 3 });
        await SettleAsync();

        await SinglePressAsync();
        Assert.NotEmpty(await CallsToAsync("OnPointerMoved"));

        await Page.Mouse.UpAsync();
    }

    [Fact]
    public async Task TenMovesDispatchedInOneFrame_ArriveAsOneMoveAtTheLastPosition()
    {
        var start = await EmptyCanvasPointAsync();
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X + 30, start.Y);
        await SettleAsync();
        var pointerId = await PressedPointerIdAsync();
        await ClearCallsAsync();

        var lastPosition = await Page.EvaluateAsync<double[]>(
            """
            (pointerId) => {
                const canvas = document.querySelector(".diagram-canvas");
                const container = document.querySelector(".diagram-container").getBoundingClientRect();
                const origin = canvas.getBoundingClientRect();
                let clientX = 0;
                let clientY = 0;
                for (let step = 1; step <= 10; step++) {
                    clientX = origin.left + 100 + step * 5;
                    clientY = origin.top + 100 + step * 3;
                    canvas.dispatchEvent(new PointerEvent("pointermove", {
                        bubbles: true,
                        cancelable: true,
                        composed: true,
                        pointerId,
                        pointerType: "mouse",
                        isPrimary: true,
                        buttons: 1,
                        clientX,
                        clientY
                    }));
                }
                return [clientX - container.left, clientY - container.top];
            }
            """,
            pointerId
        );
        await SettleAsync();

        var move = Assert.Single(await CallsToAsync("OnPointerMoved"))[0];
        Assert.Equal(lastPosition[0], move.GetProperty("x").GetDouble(), 3);
        Assert.Equal(lastPosition[1], move.GetProperty("y").GetDouble(), 3);

        await Page.Mouse.UpAsync();
    }

    // The cut-off object snapping stands down above is 3 screen pixels per millisecond: a 300 pixel
    // jump is far above it however slowly the host dispatches, and a 1 pixel step after a pause far
    // below.
    [Fact]
    public async Task ALongJumpReportsAFastPointerAndASmallStepAfterAPauseASlowOne()
    {
        var start = await EmptyCanvasPointAsync();
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(start.X - 20, start.Y - 20);
        await Page.Mouse.MoveAsync(start.X - 320, start.Y - 20);
        await SettleAsync();
        var fast = (await CallsToAsync("OnPointerMoved"))[^1][0];
        await ClearCallsAsync();

        await Task.Delay(300, TestContext.Current.CancellationToken);
        await Page.Mouse.MoveAsync(start.X - 321, start.Y - 20);
        await SettleAsync();
        var slow = Assert.Single(await CallsToAsync("OnPointerMoved"))[0];

        Assert.True(fast.GetProperty("velocity").GetDouble() > 3);
        Assert.True(slow.GetProperty("velocity").GetDouble() < 3);
        await Page.Mouse.UpAsync();
    }
}
