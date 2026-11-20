using System.Text.Json;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class ConnectorDragProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";
    private const string LooseEndId = "a0000000-0000-0000-0000-000000000003";
    private const string LabelledEdgeId = "e0000000-0000-0000-0000-000000000001";

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private async Task<(float X, float Y)> HoverLooseEndPortAsync(string part)
    {
        var port = await PortGestures.SelectAndAimAtPortAsync(Instance(LooseEndId), part);
        await Page.Mouse.MoveAsync(port.X, port.Y, new() { Steps = 4 });
        return port;
    }

    private async Task<LocatorBoundingBoxResult> SettledBoxAsync(ILocator locator)
    {
        var previous = await locator.BoundingBoxAsync();
        for (var attempt = 0; attempt < 50; attempt++)
        {
            await Task.Delay(100);
            var current = await locator.BoundingBoxAsync();
            if (
                previous is not null
                && current is not null
                && current.X == previous.X
                && current.Y == previous.Y
                && current.Width == previous.Width
            )
            {
                return current;
            }

            previous = current;
        }

        Assert.Fail("The position never settled.");
        return null!;
    }

    [Fact]
    public async Task ADropOnAnotherShapesPortPinsTheNewEdgeToIt()
    {
        var from = await HoverLooseEndPortAsync("Top");
        await ClearCallsAsync();
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(from.X + 60, from.Y - 40, new() { Steps = 4 });
        await Expect(Page.Locator(".connector-drag-preview")).ToHaveCountAsync(1);

        await PortGestures.ArriveAtPortAsync(Page, Instance(TargetId), "Bottom");
        await Page.Mouse.UpAsync();
        await SettleAsync();

        var release = Assert.Single(await CallsToAsync("OnPointerReleased"))[0];
        var topmost = release
            .GetProperty("hits")
            .EnumerateArray()
            .First(hit => hit.GetProperty("role").GetString() is "port" or "instance");
        Assert.Equal("port", topmost.GetProperty("role").GetString());
        Assert.Equal(TargetId, topmost.GetProperty("entityId").GetString());
        Assert.Equal("Bottom", topmost.GetProperty("part").GetString());

        await Expect(Page.Locator(".edge-hit")).ToHaveCountAsync(3);
        await Expect(Page.Locator(".floating-endpoint")).ToHaveCountAsync(1);
        await Expect(Page.Locator(".connector-drag-preview")).ToHaveCountAsync(0);
        await ExpectNothingRespondsToAButtonlessMoveAsync();
    }

    [Fact]
    public async Task AReleaseOutsideTheCanvasEndsTheDragAndABareMoveDrawsNoLine()
    {
        await HoverLooseEndPortAsync("Bottom");
        await ClearCallsAsync();
        var outside = await PagePointOnCanvasAsync(120, 620);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(outside.X, outside.Y, new() { Steps = 6 });
        await Page.Mouse.UpAsync();
        await SettleAsync();

        Assert.Single(await CallsToAsync("OnPointerReleased"));
        await Expect(Page.Locator(".connector-drag-preview")).ToHaveCountAsync(0);

        await ExpectNothingRespondsToAButtonlessMoveAsync();
        await Expect(Page.Locator(".connector-drag-preview")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task AClickBesideAnEdgesLineSelectsItWhenZoomedOut()
    {
        var empty = await EmptyCanvasPointAsync();
        await Page.Mouse.ClickAsync(empty.X, empty.Y);
        var scale = 1.0;
        while (scale > 0.5)
        {
            await Page.Keyboard.PressAsync("PageDown");
            await SettleAsync();
            scale = await Page.EvaluateAsync<double>(
                "() => parseFloat(getComputedStyle(document.querySelector('.canvas-content')).getPropertyValue('--d12-scale'))"
            );
        }

        Assert.True(scale > 0.2, $"One step zoomed out as far as {scale}.");
        var edge = Page.Locator($"[data-d12-role='edge'][data-d12-entity='{LabelledEdgeId}']");
        await SettledBoxAsync(edge);
        var onLine = await edge.EvaluateAsync<JsonElement>(
            """
            (line) => {
                const point = line.ownerSVGElement.createSVGPoint();
                point.x = 225;
                point.y = 90;
                const screen = point.matrixTransform(line.getScreenCTM());
                return { x: screen.x, y: screen.y };
            }
            """
        );
        await Page.Mouse.ClickAsync(
            onLine.GetProperty("x").GetSingle(),
            onLine.GetProperty("y").GetSingle() + 6
        );

        await Expect(
                Page.Locator($".edge-halo + .edge-line + [data-d12-entity='{LabelledEdgeId}']")
            )
            .ToHaveCountAsync(1);
    }

    [Fact]
    public async Task ADoubleClickOnAnEdgesLabelOpensItsEditor()
    {
        var label = await CentreOfAsync(Page.Locator(".edge-label"));
        await Page.Mouse.DblClickAsync(label.X, label.Y);

        await Expect(Page.Locator(".edge-label textarea")).ToHaveCountAsync(1);
    }
}
