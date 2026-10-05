using Microsoft.Playwright;

namespace D12Canvas.VisualTests;

// Each case starts a middle-button pan, ends the press one way, then lets the button up and
// checks that a pointer with no button held moves nothing.
public sealed class ReleaseChannelProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    [Fact]
    public async Task CaptureHoldsThroughAReleaseOutsideTheCanvas()
    {
        var start = await EmptyCanvasPointAsync();
        var outside = await PagePointOnCanvasAsync(560, 600);
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync(MiddleDown);
        await Page.Mouse.MoveAsync(outside.X, outside.Y, new() { Steps = 6 });
        await SettleAsync();

        var moves = await CallsToAsync("OnPointerMoved");
        Assert.Contains(moves, move => move[0].GetProperty("y").GetDouble() > 500);

        await Page.Mouse.UpAsync(MiddleUp);
        await SettleAsync();
        Assert.Single(await CallsToAsync("OnPointerReleased"));
        Assert.Empty(await CallsToAsync("OnPointerCancelled"));

        await ExpectNothingRespondsToAButtonlessMoveAsync();
    }

    [Theory]
    [InlineData("pointerup")]
    [InlineData("pointercancel")]
    [InlineData("blur")]
    public async Task EachReleaseChannelEndsTheGesture(string channel)
    {
        var pointerId = await StartMiddlePanAsync();

        switch (channel)
        {
            case "pointerup":
                await Page.Mouse.UpAsync(MiddleUp);
                await SettleAsync();
                Assert.Single(await CallsToAsync("OnPointerReleased"));
                break;
            case "pointercancel":
                await Page.EvaluateAsync(
                    """
                    (pointerId) => document.querySelector(".diagram-canvas").dispatchEvent(
                        new PointerEvent("pointercancel", { bubbles: true, pointerId, pointerType: "mouse" })
                    )
                    """,
                    pointerId
                );
                await ExpectCancelledByAsync(channel);
                break;
            case "blur":
                await Page.EvaluateAsync("() => window.dispatchEvent(new Event('blur'))");
                await ExpectCancelledByAsync(channel);
                break;
        }

        await ExpectNothingRespondsToAButtonlessMoveAsync();
    }

    // The browser still holds the button after a cancel, so it is let up before the check, and
    // that release must reach no one either.
    private async Task ExpectCancelledByAsync(string channel)
    {
        await SettleAsync();
        var cancel = Assert.Single(await CallsToAsync("OnPointerCancelled"));
        Assert.Equal(channel, cancel[0].GetString());

        await Page.Mouse.UpAsync(MiddleUp);
        await SettleAsync();
        Assert.Empty(await CallsToAsync("OnPointerReleased"));
    }
}
