using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A real Tab press, not an invoked focus handler, has to carry focus from a shape to the proxy
// stop of the edge leaving it, and landing there has to select that edge alone.
public sealed class EdgeTabStopProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SourceId = "a0000000-0000-0000-0000-000000000001";
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator SelectedEdgeLines => Page.Locator(".edge-line[aria-selected='true']");

    [Fact]
    public async Task TabFromAShapeLandsOnTheEdgeLeavingItAndSelectsThatEdge()
    {
        await Instance(SourceId).FocusAsync();
        await Expect(Instance(SourceId)).ToHaveAttributeAsync("aria-selected", "true");

        await Page.Keyboard.PressAsync("Tab");
        await SettleAsync();

        var focused = Page.Locator(":focus");
        await Expect(focused).ToHaveClassAsync("edge-tab-stop");
        await Expect(focused).ToHaveAttributeAsync("aria-selected", "true");
        Assert.StartsWith("Connector from ", await focused.GetAttributeAsync("aria-label"));
        await Expect(SelectedEdgeLines).ToHaveCountAsync(1);
        await Expect(Page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(0);

        await Page.Keyboard.PressAsync("Tab");
        await SettleAsync();

        await Expect(Instance(TargetId)).ToBeFocusedAsync();
        await Expect(SelectedEdgeLines).ToHaveCountAsync(0);
    }
}
