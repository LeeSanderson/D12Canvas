namespace D12Canvas.VisualTests;

// The browser moves focus to a focusable element on mousedown unless the pointerdown before it
// was default-prevented. The same focusable element is pressed in two places: outside the canvas,
// where nothing claims the press and the browser focuses it, and inside the canvas's content,
// where the walk finds no role marker and the listener claims the press as bare canvas. Nothing
// else on either path prevents a default, so the difference is the listener's pointerdown alone.
public sealed class PressFocusProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    [Fact]
    public async Task AnUnclaimedPressOnAFocusableElement_MovesFocusToIt()
    {
        var target = await AddFocusTargetAsync(
            "body",
            "position: fixed; right: 20px; bottom: 20px;"
        );

        await PressAndHoldAsync(target);

        Assert.Equal(["focus-target"], await FocusedSincePressAsync());
        await Page.Mouse.UpAsync();
    }

    [Fact]
    public async Task AClaimedPressOnAFocusableElement_FocusesTheCanvasAndNeverTheElement()
    {
        var target = await AddFocusTargetAsync(
            ".canvas-content",
            "position: absolute; left: 560px; top: 400px;"
        );

        await PressAndHoldAsync(target);
        await SettleAsync();

        Assert.Equal(["diagram-canvas"], await FocusedSincePressAsync());
        await Page.Mouse.UpAsync();
    }

    private async Task<(float X, float Y)> AddFocusTargetAsync(string parent, string placement)
    {
        await Page.EvaluateAsync(
            """
            ([parent, placement]) => {
                const target = document.createElement("div");
                target.className = "focus-target";
                target.tabIndex = 0;
                target.style.cssText = placement + " width: 80px; height: 40px;";
                document.querySelector(parent).append(target);
                window.__d12Focused = [];
                document.addEventListener("focusin", event => {
                    window.__d12Focused.push(event.target.classList[0] ?? event.target.tagName);
                }, true);
            }
            """,
            new[] { parent, placement }
        );
        return await CentreOfAsync(Page.Locator(".focus-target"));
    }

    private async Task PressAndHoldAsync((float X, float Y) point)
    {
        await Page.Mouse.MoveAsync(point.X, point.Y);
        await Page.Mouse.DownAsync();
    }

    private Task<string[]> FocusedSincePressAsync() =>
        Page.EvaluateAsync<string[]>("() => window.__d12Focused");
}
