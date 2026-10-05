namespace D12Canvas.VisualTests;

// The keydown listener sits on window, so its focus guard decides whether a key pressed somewhere
// on the host page reaches the canvas. Every probe presses PageDown away from any editable
// element, so the row's typing guard never fires and whether OnZoomOut arrives is the focus
// guard's answer alone.
public sealed class KeyboardFocusGuardProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    [Fact]
    public async Task AKeyPressedOnAHostButtonOutsideTheCanvas_NeverReachesTheCanvas()
    {
        await Page.EvaluateAsync(
            """
            () => {
                const button = document.createElement("button");
                button.textContent = "Host button";
                document.body.append(button);
                button.focus();
            }
            """
        );

        await PressPageDownAsync();

        Assert.Empty(await CallsToAsync("OnZoomOut"));
    }

    [Fact]
    public async Task AKeyPressedWithNothingFocused_ReachesTheCanvas()
    {
        await Page.EvaluateAsync("() => document.activeElement?.blur()");

        await PressPageDownAsync();

        Assert.Single(await CallsToAsync("OnZoomOut"));
    }

    [Fact]
    public async Task AKeyPressedWithNothingFocusedButHostTextSelected_NeverReachesTheCanvas()
    {
        await Page.EvaluateAsync(
            """
            () => {
                document.activeElement?.blur();
                const paragraph = document.createElement("p");
                paragraph.textContent = "Host page text the user has selected.";
                document.body.append(paragraph);
                const range = document.createRange();
                range.selectNodeContents(paragraph);
                window.getSelection().removeAllRanges();
                window.getSelection().addRange(range);
            }
            """
        );

        await PressPageDownAsync();

        Assert.Empty(await CallsToAsync("OnZoomOut"));
    }

    [Fact]
    public async Task AKeyPressedWithFocusInsideTheCanvas_ReachesTheCanvas()
    {
        await Page.EvaluateAsync("() => document.querySelector('.diagram-canvas').focus()");

        await PressPageDownAsync();

        Assert.Single(await CallsToAsync("OnZoomOut"));
    }

    private async Task PressPageDownAsync()
    {
        await ClearCallsAsync();
        await Page.Keyboard.PressAsync("PageDown");
        await SettleAsync();
    }
}
