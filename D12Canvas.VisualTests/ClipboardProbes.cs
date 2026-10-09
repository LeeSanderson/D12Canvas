using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The clipboard keys ride the browser's copy, cut and paste events, whose clipboardData is written
// and read inside the event, and the menu rows ride the async clipboard after a hop through .NET.
// Both depend on what the browser does with real key presses and real clicks, which bUnit never
// sees.
public sealed class ClipboardProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SourceId = "a0000000-0000-0000-0000-000000000001";
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";
    private const int SeededInstances = 4;

    private static readonly string[] SeededIds =
    [
        SourceId,
        TargetId,
        "a0000000-0000-0000-0000-000000000003",
        "a0000000-0000-0000-0000-000000000004",
    ];

    private static readonly LocatorClickOptions RightClick = new() { Button = MouseButton.Right };

    private ILocator Instances => Page.Locator(".component-container");

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private Task GrantClipboardAsync(params string[] permissions) =>
        Context.GrantPermissionsAsync(
            permissions.Length == 0 ? ["clipboard-read", "clipboard-write"] : permissions,
            new() { Origin = DemoAppFixture.BaseUrl }
        );

    private Task<string> ClipboardTextAsync(IPage page) =>
        page.EvaluateAsync<string>("() => navigator.clipboard.readText()");

    private static Task PressChordAsync(IPage page, string key) =>
        page.Keyboard.PressAsync($"Control+{key}");

    private static async Task<IReadOnlyList<string?>> EntityIdsAsync(IPage page) =>
        await page.Locator(".component-container")
            .EvaluateAllAsync<string?[]>(
                "elements => elements.map(element => element.getAttribute('data-d12-entity'))"
            );

    private async Task HoverCanvasAsync(IPage page, double x, double y)
    {
        var box = await page.Locator(".diagram-container").BoundingBoxAsync();
        await page.Mouse.MoveAsync((float)(box!.X + x), (float)(box.Y + y));
    }

    [Fact]
    public async Task CtrlCThenCtrlVInAnotherTab_PastesTheSelectionWithFreshIds()
    {
        await GrantClipboardAsync();
        await Instance(SourceId).ClickAsync();

        await PressChordAsync(Page, "c");

        using var payload = JsonDocument.Parse(await ClipboardTextAsync(Page));
        Assert.Equal(1, payload.RootElement.GetProperty("SchemaVersion").GetInt32());
        Assert.Equal(
            SourceId,
            payload.RootElement.GetProperty("Components")[0].GetProperty("Id").GetString()
        );

        var otherTab = await Context.NewPageAsync();
        await otherTab.GotoAsync(ProbePagePath);
        await Expect(otherTab.Locator(".component-container")).ToHaveCountAsync(SeededInstances);
        await otherTab.Locator(".component-container").First.ClickAsync();
        await HoverCanvasAsync(otherTab, 560, 420);

        await PressChordAsync(otherTab, "v");

        await Expect(otherTab.Locator(".component-container"))
            .ToHaveCountAsync(SeededInstances + 1);
        var pastedId = (await EntityIdsAsync(otherTab)).Except(SeededIds).Single();
        await Expect(otherTab.Locator($"[data-d12-entity='{pastedId}']"))
            .ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task FivePastesAtOneSpot_CascadeAndMovingThePointerStartsAgain()
    {
        await GrantClipboardAsync();
        await Instance(SourceId).ClickAsync();
        await PressChordAsync(Page, "c");
        await HoverCanvasAsync(Page, 400, 300);

        for (var i = 0; i < 5; i++)
        {
            await PressChordAsync(Page, "v");
            await Expect(Instances).ToHaveCountAsync(SeededInstances + i + 1);
        }

        var lefts = await PastedLeftsAsync();
        await HoverCanvasAsync(Page, 300, 300);
        await PressChordAsync(Page, "v");
        await Expect(Instances).ToHaveCountAsync(SeededInstances + 6);
        var afterMove = await PastedLeftsAsync();

        Assert.Equal(5, lefts.Distinct().Count());
        Assert.Equal(lefts.Order(), lefts);
        Assert.Contains(afterMove.Except(lefts), left => left < lefts.Min());
    }

    private async Task<IReadOnlyList<double>> PastedLeftsAsync() =>
        await Page.EvaluateAsync<double[]>(
            """
            (seeded) => [...document.querySelectorAll(".component-container")]
                .filter(element => !seeded.includes(element.getAttribute("data-d12-entity")))
                .map(element => element.getBoundingClientRect().left)
            """,
            SeededIds
        );

    [Fact]
    public async Task CtrlXRemovesTheSelectionAndCtrlVPutsItBack()
    {
        await GrantClipboardAsync();
        await Instance(SourceId).ClickAsync();

        await PressChordAsync(Page, "x");

        await Expect(Instances).ToHaveCountAsync(SeededInstances - 1);
        await Expect(Instance(SourceId)).ToHaveCountAsync(0);

        await HoverCanvasAsync(Page, 400, 300);
        await PressChordAsync(Page, "v");

        await Expect(Instances).ToHaveCountAsync(SeededInstances);
        await Expect(Instance(SourceId)).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task PastingPlainText_CreatesATextShapeHoldingIt()
    {
        await GrantClipboardAsync();
        await Page.EvaluateAsync("() => navigator.clipboard.writeText('Pasted from elsewhere')");
        await Instance(SourceId).ClickAsync();
        await HoverCanvasAsync(Page, 400, 300);

        await PressChordAsync(Page, "v");

        await Expect(Page.Locator(".component-container[aria-label='Text'] .d12-text").Last)
            .ToHaveTextAsync("Pasted from elsewhere");
    }

    [Fact]
    public async Task CtrlCInAHostInputOutsideTheCanvas_CopiesTheInputsOwnText()
    {
        await GrantClipboardAsync();
        await Instance(SourceId).ClickAsync();
        await Page.EvaluateAsync(
            """
            () => {
                const input = document.createElement("input");
                input.className = "probe-host-input";
                input.value = "host words";
                document.body.append(input);
                input.focus();
                input.select();
            }
            """
        );

        await PressChordAsync(Page, "c");

        Assert.Equal("host words", await ClipboardTextAsync(Page));
    }

    // Chromium grants a focused page the right to write without asking, so whether the write
    // succeeded says nothing about activation; the page's own record of it at the moment of the
    // write does.
    [Fact]
    public async Task TheMenusCopyRow_WritesWhileTheClicksUserActivationIsStillLive()
    {
        await GrantClipboardAsync();
        await Page.EvaluateAsync(
            """
            () => {
                const write = navigator.clipboard.writeText.bind(navigator.clipboard);
                window.__clipboardWrites = [];
                navigator.clipboard.writeText = (text) => {
                    window.__clipboardWrites.push(navigator.userActivation.isActive);
                    return write(text);
                };
            }
            """
        );
        await Instance(SourceId).ClickAsync(RightClick);
        var copyRow = Page.Locator(".d12-context-menu-item", new() { HasText = "Copy" });
        await Expect(copyRow).ToBeVisibleAsync();

        await copyRow.ClickAsync();
        await SettleAsync();

        var activationAtEachWrite = await Page.EvaluateAsync<bool[]>(
            "() => window.__clipboardWrites"
        );
        Assert.Equal(new[] { true }, activationAtEachWrite);
        Assert.Contains(SourceId, await ClipboardTextAsync(Page));
    }

    [Fact]
    public async Task TheMenusPasteRow_LandsAtThePressThatOpenedTheMenu()
    {
        await GrantClipboardAsync();
        await Page.EvaluateAsync("() => navigator.clipboard.writeText('Menu paste')");
        var pressPoint = await PagePointOnBoardAsync(300, 440);
        await Page.Mouse.ClickAsync(
            pressPoint.X,
            pressPoint.Y,
            new() { Button = MouseButton.Right }
        );
        var pasteRow = Page.Locator(".d12-context-menu-item", new() { HasText = "Paste" });

        await pasteRow.ClickAsync();

        var text = Page.Locator(".component-container[aria-label='Text']").Last;
        await Expect(text.Locator(".d12-text")).ToHaveTextAsync("Menu paste");
        await Expect(text)
            .ToHaveAttributeAsync("style", new Regex("^left: 200px; top: 420px; width: 200px;"));
    }
}
