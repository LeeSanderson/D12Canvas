using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// An author's control inside a group that is not entered does nothing: C# renders a marker on the
// member's container and the listener's walk reads it, so the press classifies as the instance and
// the browser never delivers the control its click. Once the group is entered the same control
// answers the press itself. The first click selects the group, so the double-click to enter it
// lands on the selection box drawn over the group.
public sealed class EnteredGroupProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";

    protected override string ProbePageQuery => "?group=true";

    [Fact]
    public async Task AButtonInsideAGroupThatIsNotEnteredDoesNothingUntilTheGroupIsEntered()
    {
        var target = Page.Locator($".component-container[data-d12-entity='{TargetId}']");
        await Expect(target).ToHaveAttributeAsync("data-d12-unaddressable", "true");
        await InjectButtonIntoTargetAsync();
        var button = Page.Locator(".probe-author-button");

        await button.ClickAsync();
        await SettleAsync();

        Assert.Equal(0, await ButtonClickCountAsync());
        Assert.Equal("instance", (await SinglePressAsync()).GetProperty("role").GetString());

        var targetBox = await target.BoundingBoxAsync();
        Assert.NotNull(targetBox);
        await Page.Mouse.DblClickAsync(targetBox!.X + 140, targetBox.Y + 80);
        await Expect(target).Not.ToHaveAttributeAsync("data-d12-unaddressable", "true");
        await SettleAsync();
        await ClearCallsAsync();

        await button.ClickAsync();
        await SettleAsync();

        Assert.Equal(1, await ButtonClickCountAsync());
        Assert.Equal("author-content", (await SinglePressAsync()).GetProperty("role").GetString());
    }

    private Task InjectButtonIntoTargetAsync() =>
        Page.EvaluateAsync(
            """
            (selector) => {
                window.__probeButtonClicks = 0;
                const host = document.querySelector(selector);
                const button = document.createElement("button");
                button.className = "probe-author-button";
                button.textContent = "press";
                button.style.cssText =
                    "position: absolute; left: 10px; top: 10px; width: 80px; height: 30px; z-index: 1;";
                button.addEventListener("click", () => {
                    window.__probeButtonClicks += 1;
                });
                host.appendChild(button);
            }
            """,
            $"[data-d12-entity='{TargetId}'] .container-content"
        );

    private Task<int> ButtonClickCountAsync() =>
        Page.EvaluateAsync<int>("() => window.__probeButtonClicks");
}
