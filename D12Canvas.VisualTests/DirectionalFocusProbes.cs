using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Real Ctrl+Shift+Arrow presses, since only a real key shows the browser lets the chord through
// and the stop's own focus handler takes the landing. On the probe board Target sits in Source's
// row and LooseEnd in its column, with the edge leaving Source between Source and Target in the
// ring.
public sealed class DirectionalFocusProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SourceId = "a0000000-0000-0000-0000-000000000001";
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";
    private const string LooseEndId = "a0000000-0000-0000-0000-000000000003";

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator SelectedInstances =>
        Page.Locator(".component-container[aria-selected='true']");

    private async Task PressAsync(string key)
    {
        await Page.Keyboard.PressAsync(key);
        await SettleAsync();
    }

    [Fact]
    public async Task CtrlShiftArrowMovesFocusAndSelectionToTheNeighbourAndStopsAtTheEdgeOfTheRing()
    {
        await Instance(SourceId).FocusAsync();

        await PressAsync("Control+Shift+ArrowRight");

        await Expect(Instance(TargetId)).ToBeFocusedAsync();
        await Expect(SelectedInstances).ToHaveCountAsync(1);
        await Expect(Instance(TargetId)).ToHaveAttributeAsync("aria-selected", "true");

        await PressAsync("Control+Shift+ArrowUp");

        await Expect(Instance(TargetId)).ToBeFocusedAsync();
        await Expect(Instance(TargetId)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task AMoveThatFindsNoStopStillPreventsTheBrowsersDefault()
    {
        await Page.EvaluateAsync(
            """
            () => {
                window.directionalFocusDefaults = [];
                window.addEventListener("keydown", (event) => {
                    if (event.code === "ArrowLeft") {
                        window.directionalFocusDefaults.push(event.defaultPrevented);
                    }
                });
            }
            """
        );
        await Instance(SourceId).FocusAsync();

        await PressAsync("Control+Shift+ArrowLeft");

        await Expect(Instance(SourceId)).ToBeFocusedAsync();
        var defaults = await Page.EvaluateAsync<bool[]>("() => window.directionalFocusDefaults");
        Assert.Equal([true], defaults);
    }

    [Fact]
    public async Task InsideAdditiveTraversalTheMoveLeavesTheSelectionAlone()
    {
        await Instance(SourceId).FocusAsync();

        await PressAsync("Space");
        await PressAsync("Control+Shift+ArrowDown");

        await Expect(Instance(LooseEndId)).ToBeFocusedAsync();
        await Expect(SelectedInstances).ToHaveCountAsync(1);
        await Expect(Instance(SourceId)).ToHaveAttributeAsync("aria-selected", "true");
    }
}
