using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Real Tab, Space and Escape presses, never an invoked handler, since only a real key shows the
// browser lets it through. The probe board's ring reads Source, the edge leaving it, Target,
// Note, LooseEnd, the edge leaving LooseEnd.
public sealed class AdditiveTraversalProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SourceId = "a0000000-0000-0000-0000-000000000001";
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";
    private const string LooseEndId = "a0000000-0000-0000-0000-000000000003";
    private const string NoteId = "a0000000-0000-0000-0000-000000000004";

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator SelectedInstances =>
        Page.Locator(".component-container[aria-selected='true']");

    private ILocator SelectedEdgeLines => Page.Locator(".edge-line[aria-selected='true']");

    private async Task PressAsync(string key)
    {
        await Page.Keyboard.PressAsync(key);
        await SettleAsync();
    }

    [Fact]
    public async Task SpaceAndTabBuildASelectionThatEscapeKeepsAndAPlainTabReplaces()
    {
        await Instance(SourceId).FocusAsync();

        await PressAsync("Space");
        await PressAsync("Tab");
        await PressAsync("Tab");
        await PressAsync("Space");

        await Expect(Instance(TargetId)).ToBeFocusedAsync();
        await Expect(SelectedInstances).ToHaveCountAsync(2);
        await Expect(Instance(SourceId)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Instance(TargetId)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(SelectedEdgeLines).ToHaveCountAsync(0);

        await PressAsync("Escape");

        await Expect(SelectedInstances).ToHaveCountAsync(2);

        await PressAsync("Tab");

        await Expect(Instance(NoteId)).ToBeFocusedAsync();
        await Expect(SelectedInstances).ToHaveCountAsync(1);
        await Expect(Instance(NoteId)).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task TabbingPastTheLastStopAndBackInEndsTheMode()
    {
        await Page.EvaluateAsync(
            """
            () => {
                const button = document.createElement("button");
                button.textContent = "Host button";
                document.body.append(button);
            }
            """
        );
        await Instance(LooseEndId).FocusAsync();

        await PressAsync("Space");
        await PressAsync("Tab");
        await Expect(Page.Locator(".edge-tab-stop:focus")).ToHaveCountAsync(1);
        await PressAsync("Tab");
        await Expect(Page.Locator("body > button")).ToBeFocusedAsync();

        await PressAsync("Shift+Tab");

        await Expect(Page.Locator(".edge-tab-stop:focus")).ToHaveCountAsync(1);
        await Expect(SelectedEdgeLines).ToHaveCountAsync(1);
        await Expect(SelectedInstances).ToHaveCountAsync(0);
    }
}
