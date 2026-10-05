using Microsoft.Playwright;
using Xunit.Sdk;

namespace D12Canvas.VisualTests;

// The browser applies a released capture at the next pointer event, so the probe moves the
// pointer once after releasing it.
public sealed class LostCaptureProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    [Fact]
    public async Task LosingCaptureOnALiveGesture_LogsAConsoleErrorThatFailsTheTest()
    {
        var pointerId = await StartMiddlePanAsync();
        var start = await EmptyCanvasPointAsync();

        await Page.RunAndWaitForConsoleMessageAsync(
            async () =>
            {
                await Page.EvaluateAsync(
                    "(pointerId) => document.querySelector('.diagram-canvas').releasePointerCapture(pointerId)",
                    pointerId
                );
                await Page.Mouse.MoveAsync(start.X - 50, start.Y - 30);
            },
            new() { Predicate = message => message.Type == "error" }
        );
        await SettleAsync();

        var cancel = Assert.Single(await CallsToAsync("OnPointerCancelled"));
        Assert.Equal("lostpointercapture", cancel[0].GetString());
        Assert.Throws<FailException>(ConsoleErrors.AssertNone);
        ConsoleErrors.Clear();

        await Page.Mouse.UpAsync(MiddleUp);
        await ExpectNothingRespondsToAButtonlessMoveAsync();
    }
}
