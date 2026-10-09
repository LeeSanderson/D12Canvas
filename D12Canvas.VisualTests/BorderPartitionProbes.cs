using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The border partition in a real browser, on a board of three rectangles: a small one at the
// origin, one with six custom ports crowding its top side, and a large one. Ports show on a
// selected shape and never on hover; a side splits between a port span around each port and
// resize spans nearer the corners, told apart only by the cursor; spans measured in screen pixels
// drop out when zoomed out too far and come back when zoomed in, while corner handles stay.
public sealed class BorderPartitionProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string SmallId = "b0000000-0000-0000-0000-000000000001";
    private const string CrowdedId = "b0000000-0000-0000-0000-000000000002";
    private const string LargeId = "b0000000-0000-0000-0000-000000000003";

    protected override string ProbePagePath => "/border-partition-demo";

    protected override int ProbePageInstanceCount => 3;

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private async Task<double> ScaleAsync() =>
        await Page.EvaluateAsync<double>(
            "() => parseFloat(getComputedStyle(document.querySelector('.canvas-content')).getPropertyValue('--d12-scale'))"
        );

    private Task<(float X, float Y)> PageAtAsync(double boardX, double boardY) =>
        PagePointOnBoardAsync(boardX, boardY);

    private async Task<string> CursorAtAsync((float X, float Y) point) =>
        await Page.EvaluateAsync<string>(
            "({ x, y }) => getComputedStyle(document.elementFromPoint(x, y)).cursor",
            new { x = (double)point.X, y = (double)point.Y }
        );

    private async Task SelectAsync(string id, double boardX, double boardY)
    {
        var point = await PageAtAsync(boardX, boardY);
        await Page.Mouse.ClickAsync(point.X, point.Y);
        await Expect(Instance(id)).ToHaveAttributeAsync("aria-selected", "true");
    }

    private async Task FocusTheCanvasAsync()
    {
        var corner = await PagePointOnCanvasAsync(5, 550);
        await Page.Mouse.ClickAsync(corner.X, corner.Y);
    }

    // Each key steps the zoom by a tenth; the content eases into place, so this waits for a shape
    // still on screen, the large one by default, to reach its new on-screen width.
    private async Task ZoomByKeysAsync(
        string key,
        int presses,
        string watchedId = LargeId,
        double watchedWidth = 340
    )
    {
        for (var i = 0; i < presses; i++)
        {
            await Page.Keyboard.PressAsync(key);
        }

        var scale = await ScaleAsync();
        await GestureWaits.UntilBoxAsync(
            Instance(watchedId),
            box => Math.Abs(box.Width - watchedWidth * scale) < 1,
            $"the shape settling at scale {scale}"
        );
    }

    // The board opens fitted just below 100%, so the zoom steps start from 100% to land where
    // these probes measure.
    private async Task ToHundredPercentAsync()
    {
        await Page.Keyboard.PressAsync("Shift+Digit0");
        await GestureWaits.UntilBoxAsync(
            Instance(LargeId),
            box => Math.Abs(box.Width - 340) < 1,
            "the large shape at 100%"
        );
    }

    private async Task DragAsync((float X, float Y) from, float dx, float dy)
    {
        await Page.Mouse.MoveAsync(from.X, from.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(from.X + dx, from.Y + dy, new() { Steps = 4 });
        await Page.Mouse.UpAsync();
        await SettleAsync();
    }

    private async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        return box!;
    }

    [Fact]
    public async Task HoveringAShapeShowsNoPorts()
    {
        var centre = await CentreOfAsync(Instance(LargeId));
        await Page.Mouse.MoveAsync(centre.X, centre.Y, new() { Steps = 4 });
        await SettleAsync();

        await Expect(Page.Locator(".port")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".port-span")).ToHaveCountAsync(0);
    }

    // The large rectangle runs from (520, 40) to (860, 340). Its top side holds the standard port
    // around x 690 and resize spans either side of it; its left side likewise around y 190.
    public static TheoryData<string, double, double, string> PartitionCursors =>
        new()
        {
            { "the top port's span", 690, 40, "crosshair" },
            { "the top side between corner and port", 580, 40, "ns-resize" },
            { "the left port's span", 520, 190, "crosshair" },
            { "the left side between corner and port", 520, 100, "ew-resize" },
            { "the top-left corner", 520, 40, "nwse-resize" },
            { "the top-right corner", 860, 40, "nesw-resize" },
            { "the bottom-right corner", 860, 340, "nwse-resize" },
            { "the body", 690, 190, "move" },
        };

    [Theory]
    [MemberData(nameof(PartitionCursors))]
    public async Task TheCursorAloneDrawsThePartition(
        string region,
        double boardX,
        double boardY,
        string cursor
    )
    {
        await SelectAsync(LargeId, 690, 190);

        Assert.True(
            await CursorAtAsync(await PageAtAsync(boardX, boardY)) == cursor,
            $"Over {region} the cursor was not {cursor}."
        );
    }

    [Fact]
    public async Task PressingNearASidesCentrePullsAConnectorAndNearerACornerResizes()
    {
        await SelectAsync(LargeId, 690, 190);

        var port = await PageAtAsync(690, 40);
        await Page.Mouse.MoveAsync(port.X, port.Y);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(port.X, port.Y - 30, new() { Steps = 4 });
        await Expect(Page.Locator(".connector-drag-preview")).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Escape");
        await Page.Mouse.UpAsync();
        await SettleAsync();
        var before = await BoxOfAsync(Instance(LargeId));

        await DragAsync(await PageAtAsync(580, 40), 0, -30);

        await Expect(Page.Locator(".edge-line")).ToHaveCountAsync(0);
        var after = await BoxOfAsync(Instance(LargeId));
        Assert.Equal(before.Y - 30, after.Y, 1);
        Assert.Equal(before.Height + 30, after.Height, 1);
    }

    [Fact]
    public async Task ACornerHandleIsGrabbableZoomedOutToATenth()
    {
        await FocusTheCanvasAsync();
        await ToHundredPercentAsync();
        await ZoomByKeysAsync("PageDown", 9);
        await SelectAsync(LargeId, 690, 190);
        var before = await BoxOfAsync(Instance(LargeId));

        await DragAsync(await PageAtAsync(860, 340), 20, 20);

        var after = await BoxOfAsync(Instance(LargeId));
        Assert.True(
            after.Width > before.Width + 10,
            $"Width went {before.Width} to {after.Width}."
        );
        Assert.Equal(before.X, after.X, 1);
    }

    [Fact]
    public async Task ACornerHandleIsGrabbableZoomedInFourTimes()
    {
        await FocusTheCanvasAsync();
        // The zoom keys scale about the container's corner, so the small shape is panned up
        // toward it first and stays on screen at four times.
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("ArrowDown");
        await ZoomByKeysAsync("PageUp", 30, SmallId, 100);
        await SelectAsync(SmallId, 70, 70);
        var before = await BoxOfAsync(Instance(SmallId));

        await DragAsync(await PageAtAsync(120, 120), 40, 40);

        var after = await BoxOfAsync(Instance(SmallId));
        Assert.Equal(before.Width + 40, after.Width, 1);
        Assert.Equal(before.Height + 40, after.Height, 1);
    }

    [Fact]
    public async Task ACornerHandleIsGrabbableBesideSixCustomPorts()
    {
        await SelectAsync(CrowdedId, 310, 110);
        var before = await BoxOfAsync(Instance(CrowdedId));

        await DragAsync(await PageAtAsync(160, 40), -30, -20);

        await Expect(Page.Locator(".edge-line")).ToHaveCountAsync(0);
        var after = await BoxOfAsync(Instance(CrowdedId));
        Assert.Equal(before.Width + 30, after.Width, 1);
        Assert.Equal(before.Height + 20, after.Height, 1);
    }

    [Fact]
    public async Task AResizeSpanBelowTheFloorLeavesRenderAndHitAndZoomingInBringsItBack()
    {
        await SelectAsync(LargeId, 690, 190);
        await Expect(Instance(LargeId).Locator(".resize-span")).ToHaveCountAsync(8);
        await ToHundredPercentAsync();

        await ZoomByKeysAsync("PageDown", 8);

        await Expect(Instance(LargeId).Locator(".resize-span")).ToHaveCountAsync(0);
        await Expect(Instance(LargeId).Locator(".port-span")).ToHaveCountAsync(4);
        await Expect(Instance(LargeId).Locator(".resize-handle")).ToHaveCountAsync(4);
        var betweenCornerAndRun = await PageAtAsync(520 + 90, 40);
        Assert.NotEqual(
            "resize-handle",
            await Page.EvaluateAsync<string?>(
                "({ x, y }) => document.elementFromPoint(x, y)?.getAttribute('data-d12-role')",
                new { x = (double)betweenCornerAndRun.X, y = (double)betweenCornerAndRun.Y }
            )
        );

        await ZoomByKeysAsync("PageUp", 8);

        await Expect(Instance(LargeId).Locator(".resize-span")).ToHaveCountAsync(8);
    }

    // The custom port at 0.47 along the crowded top sits too close to the standard port to keep a
    // span, so it is neither drawn nor hit: a drop over it lands on the standard port instead.
    [Fact]
    public async Task ADropWhereACustomPortWasSqueezedOutPinsToTheStandardPortBesideIt()
    {
        var from = await PortGestures.SelectAndAimAtPortAsync(Instance(SmallId), "Right");
        await Page.Mouse.MoveAsync(from.X, from.Y);
        await Page.Mouse.DownAsync();
        var body = await PageAtAsync(310, 110);
        await Page.Mouse.MoveAsync(body.X, body.Y, new() { Steps = 4 });
        await Expect(Instance(CrowdedId).Locator(".port-span")).ToHaveCountAsync(8);
        await Expect(Instance(CrowdedId).Locator(".custom-port")).ToHaveCountAsync(4);

        var squeezedOut = await PageAtAsync(160 + 0.47 * 300, 40);
        await Page.Mouse.MoveAsync(squeezedOut.X, squeezedOut.Y, new() { Steps = 2 });
        await Page.Mouse.UpAsync();

        var line = Page.Locator(".edge-line");
        await Expect(line).ToHaveAttributeAsync("x2", "310");
        await Expect(line).ToHaveAttributeAsync("y2", "40");
    }
}
