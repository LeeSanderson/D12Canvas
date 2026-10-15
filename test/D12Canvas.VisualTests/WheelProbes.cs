using Microsoft.Playwright;

namespace D12Canvas.VisualTests;

// The wheel listener on the container cancels every wheel event over the canvas, and classifies a
// wheel gesture's granularity once, at its start, holding it until the run goes idle. The waits
// sit well clear of the idle boundary on either side rather than locating it.
public sealed class WheelProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string DispatchWheelScript = """
        async (deltas) => {
            const container = document.querySelector(".diagram-container");
            const origin = container.getBoundingClientRect();
            for (const deltaY of deltas) {
                container.dispatchEvent(new WheelEvent("wheel", {
                    bubbles: true,
                    cancelable: true,
                    deltaY,
                    clientX: origin.left + 300,
                    clientY: origin.top + 300
                }));
                await new Promise((resolve) => requestAnimationFrame(resolve));
            }
        }
        """;

    private async Task<bool[]> CoarseFlagsOfWheelCallsAsync() =>
        [
            .. (await CallsToAsync("OnWheel")).Select(args =>
                args[0].GetProperty("coarse").GetBoolean()
            ),
        ];

    [Fact]
    public async Task EveryWheelOverTheCanvasIsCancelled_PlainOrWithCtrl()
    {
        await Page.EvaluateAsync(
            """
            () => {
                window.__wheelCancelled = [];
                window.addEventListener("wheel", (event) => window.__wheelCancelled.push(event.defaultPrevented));
            }
            """
        );
        var point = await EmptyCanvasPointAsync();
        await Page.Mouse.MoveAsync(point.X, point.Y);

        await Page.Mouse.WheelAsync(0, 120);
        await Page.Keyboard.DownAsync("Control");
        await Page.Mouse.WheelAsync(0, -120);
        await Page.Keyboard.UpAsync("Control");
        await SettleAsync();

        var cancelled = await Page.EvaluateAsync<bool[]>("() => window.__wheelCancelled");
        Assert.Equal([true, true], cancelled);
        Assert.NotEmpty(await CallsToAsync("OnWheel"));
    }

    [Fact]
    public async Task AWheelGestureKeepsTheGranularityOfItsFirstEvent_UntilItGoesIdle()
    {
        await Page.EvaluateAsync(DispatchWheelScript, new[] { 100.0, 2.5, 2.5 });
        await SettleAsync();

        var firstRun = await CoarseFlagsOfWheelCallsAsync();
        Assert.NotEmpty(firstRun);
        Assert.All(firstRun, Assert.True);

        await ClearCallsAsync();
        await Page.WaitForTimeoutAsync(1500);
        await Page.EvaluateAsync(DispatchWheelScript, new[] { 2.5, 100.0 });
        await SettleAsync();

        var secondRun = await CoarseFlagsOfWheelCallsAsync();
        Assert.NotEmpty(secondRun);
        Assert.All(secondRun, Assert.False);
    }
}

// Pinned to the trackpad so a plain wheel pans with no easing, which moves what the pointer holds
// on the board and settles on screen within the frame.
public sealed class WheelUnderAPressProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    protected override string ProbePageQuery => "?wheel=trackpad";

    private ILocator Source =>
        Page.Locator(
            ".component-container[data-d12-entity='a0000000-0000-0000-0000-000000000001']"
        );

    [Fact]
    public async Task AWheelWithThePrimaryButtonHeld_ReachesTheCanvasAndCarriesTheDraggedShape()
    {
        var start = await CentreOfAsync(Source);
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        var pointer = (X: start.X + 30, Y: start.Y + 20);
        await Page.Mouse.MoveAsync(pointer.X, pointer.Y, new() { Steps = 3 });
        await SettleAsync();
        var boardPositionBefore = await Source.GetAttributeAsync("style");
        await Page.EvaluateAsync(
            """
            () => {
                window.__wheelCancelled = [];
                window.addEventListener("wheel", (event) => window.__wheelCancelled.push(event.defaultPrevented));
            }
            """
        );

        await Page.Mouse.WheelAsync(0, 120);
        await SettleAsync();

        var cancelled = await Page.EvaluateAsync<bool[]>("() => window.__wheelCancelled");
        Assert.Equal([true], cancelled);
        Assert.NotEmpty(await CallsToAsync("OnWheel"));
        Assert.NotEqual(boardPositionBefore, await Source.GetAttributeAsync("style"));
        var box = await Source.BoundingBoxAsync();
        Assert.NotNull(box);
        Assert.InRange(pointer.X, box!.X, box.X + box.Width);
        Assert.InRange(pointer.Y, box.Y, box.Y + box.Height);

        await Page.Mouse.UpAsync();
    }

    [Fact]
    public async Task APressPromotedByAWheel_ForwardsAMoveBelowTheDragThreshold()
    {
        var start = await EmptyCanvasPointAsync();
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.WheelAsync(0, 60);
        await SettleAsync();
        await ClearCallsAsync();

        await Page.Mouse.MoveAsync(start.X + 1, start.Y);
        await SettleAsync();

        Assert.NotEmpty(await CallsToAsync("OnPointerMoved"));

        await Page.Mouse.UpAsync();
    }
}
