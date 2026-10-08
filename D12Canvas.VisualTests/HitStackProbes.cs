using System.Text.Json;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The hit stack is read from the browser's own hit test at the press point, so these check that it
// crosses to C# topmost first in paint order, that an Alt click steps down it and wraps, and that
// a click inside the selection box reaches the shape beneath it.
public sealed class HitStackProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SourceId = "a0000000-0000-0000-0000-000000000001";
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";
    private const string LooseEndId = "a0000000-0000-0000-0000-000000000003";
    private const string NoteId = "a0000000-0000-0000-0000-000000000004";

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private async Task ExpectOnlySelectedAsync(string id)
    {
        await Expect(Instance(id)).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(Page.Locator(".component-container[aria-selected='true']"))
            .ToHaveCountAsync(1);
    }

    private static IReadOnlyList<string> InstancesInOrder(JsonElement press) =>
        press
            .GetProperty("hits")
            .EnumerateArray()
            .Where(hit => hit.GetProperty("role").GetString() == "instance")
            .Select(hit => hit.GetProperty("entityId").GetString()!)
            .Distinct()
            .ToList();

    private async Task AltClickAsync(float x, float y)
    {
        await Page.Keyboard.DownAsync("Alt");
        await Page.Mouse.ClickAsync(x, y);
        await Page.Keyboard.UpAsync("Alt");
        await SettleAsync();
    }

    [Fact]
    public async Task AltClicksStepDownAStackOfThreeInPaintOrderAndWrap()
    {
        await Page.GotoAsync("/interaction-probe-demo?stack=true");
        await Expect(Page.Locator(".component-container")).ToHaveCountAsync(4);
        await SettleAsync();
        var source = await Instance(SourceId).BoundingBoxAsync();
        Assert.NotNull(source);
        var x = (float)(source!.X + source.Width * 0.75);
        var y = (float)(source.Y + source.Height * 0.8);
        await ClearCallsAsync();

        await AltClickAsync(x, y);

        Assert.Equal([LooseEndId, TargetId, SourceId], InstancesInOrder(await SinglePressAsync()));
        await ExpectOnlySelectedAsync(TargetId);

        await AltClickAsync(x, y);
        await ExpectOnlySelectedAsync(SourceId);

        await AltClickAsync(x, y);
        await ExpectOnlySelectedAsync(LooseEndId);

        await AltClickAsync(x, y);
        await ExpectOnlySelectedAsync(TargetId);
    }

    [Fact]
    public async Task AClickInsideTheSelectionBoxSelectsTheShapeBeneathIt()
    {
        var source = await CentreOfAsync(Instance(SourceId));
        var note = await CentreOfAsync(Instance(NoteId));
        await Page.Mouse.ClickAsync(source.X, source.Y);
        await Page.Keyboard.DownAsync("Shift");
        await Page.Mouse.ClickAsync(note.X, note.Y);
        await Page.Keyboard.UpAsync("Shift");
        await Expect(Page.Locator(".selection-bounding-box")).ToHaveCountAsync(1);
        await SettleAsync();
        await ClearCallsAsync();

        var target = await CentreOfAsync(Instance(TargetId));
        await Page.Mouse.ClickAsync(target.X, target.Y);
        await SettleAsync();

        var press = await SinglePressAsync();
        Assert.Equal("selection-bounds", press.GetProperty("role").GetString());
        Assert.Equal([TargetId], InstancesInOrder(press));
        await ExpectOnlySelectedAsync(TargetId);
    }
}
