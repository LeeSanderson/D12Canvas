using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// Ctrl+D is a keydown row in the canvas's keyboard listener, which bUnit never runs, and it must
// keep the browser's own Ctrl+D and the system clipboard out of it.
public sealed class DuplicateProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SourceId = "a0000000-0000-0000-0000-000000000001";
    private const int SeededInstances = 4;

    private static readonly string[] SeededIds =
    [
        SourceId,
        "a0000000-0000-0000-0000-000000000002",
        "a0000000-0000-0000-0000-000000000003",
        "a0000000-0000-0000-0000-000000000004",
    ];

    private ILocator Instances => Page.Locator(".component-container");

    private ILocator Source => Page.Locator($".component-container[data-d12-entity='{SourceId}']");

    private ILocator Copies =>
        Page.Locator(
            string.Concat(
                SeededIds
                    .Select(id => $":not([data-d12-entity='{id}'])")
                    .Prepend(".component-container")
            )
        );

    private ILocator Selected => Page.Locator(".component-container[aria-selected='true']");

    private Task DuplicateAsync() => Page.Keyboard.PressAsync("Control+d");

    private static async Task<(double Left, double Top)> InlinePositionAsync(ILocator instance)
    {
        var style = await instance.GetAttributeAsync("style");
        var match = Regex.Match(style!, @"left: (-?[\d.]+)px; top: (-?[\d.]+)px");
        return (
            double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
        );
    }

    [Fact]
    public async Task CtrlD_DuplicatesBesideTheSourceAndLeavesTheClipboardAlone()
    {
        await Context.GrantPermissionsAsync(
            ["clipboard-read", "clipboard-write"],
            new() { Origin = DemoAppFixture.BaseUrl }
        );
        await Page.EvaluateAsync("() => navigator.clipboard.writeText('copied elsewhere')");
        await Source.ClickAsync();
        var source = await InlinePositionAsync(Source);

        await DuplicateAsync();

        await Expect(Instances).ToHaveCountAsync(SeededInstances + 1);
        await Expect(Copies).ToHaveAttributeAsync("aria-selected", "true");
        Assert.Equal((source.Left + 20, source.Top + 20), await InlinePositionAsync(Copies));
        Assert.Equal(
            "copied elsewhere",
            await Page.EvaluateAsync<string>("() => navigator.clipboard.readText()")
        );
    }

    [Fact]
    public async Task CtrlDThenNudgeThenCtrlD_RepeatsTheNudgedOffset()
    {
        await Source.ClickAsync();
        var source = await InlinePositionAsync(Source);
        await DuplicateAsync();
        await Expect(Instances).ToHaveCountAsync(SeededInstances + 1);
        var firstCopy = Copies;
        var firstCopyBeforeNudge = await InlinePositionAsync(firstCopy);

        await Page.Keyboard.PressAsync("Shift+ArrowRight");
        await Expect(firstCopy)
            .Not.ToHaveAttributeAsync(
                "style",
                new Regex(
                    "^left: "
                        + firstCopyBeforeNudge.Left.ToString(CultureInfo.InvariantCulture)
                        + "px;"
                )
            );
        var firstCopyAt = await InlinePositionAsync(firstCopy);
        await DuplicateAsync();

        await Expect(Instances).ToHaveCountAsync(SeededInstances + 2);
        Assert.Equal(
            (2 * firstCopyAt.Left - source.Left, 2 * firstCopyAt.Top - source.Top),
            await InlinePositionAsync(Selected)
        );
    }
}
