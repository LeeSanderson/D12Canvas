using System.Globalization;
using System.Text.RegularExpressions;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The minimap beside a canvas at the origin, 800 by 600, holding instances at (100, 100) and
// (400, 400), each 50 by 50. Measured at 200 by 150, the minimap frames the union of that content
// and the viewport, (0, 0, 800, 600), at 0.225 with the board origin at (10, 7.5), so minimap
// (55, 41.25) is board (200, 150), which centred in the canvas is pan (200, 150).
public class MinimapTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public MinimapTests()
    {
        SetupDiagramCanvasJsModule();

        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: ComponentTypeKey,
                ComponentType: typeof(TestPropsComponent),
                PropsType: typeof(TestProps),
                DisplayName: "Test Props",
                AccessibleName: "Test props component",
                DefaultProps: new TestProps(),
                Icon: null,
                Role: "group",
                DefaultSize: new ComponentSize(50, 50),
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    private static Board SeededBoard()
    {
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(100, 100, 50, 50))
        );
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(400, 400, 50, 50))
        );
        return board;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board) =>
        Render<DiagramCanvas>(parameters =>
                parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
            )
            .ReturnToOrigin();

    private async Task<IRenderedComponent<Minimap>> RenderMinimap(
        IRenderedComponent<DiagramCanvas> canvas,
        bool measured = true
    )
    {
        var minimap = Render<Minimap>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));
        if (measured)
        {
            await minimap.Resize(200, 150);
        }

        return minimap;
    }

    private static double Px(string style, string property)
    {
        var match = Regex.Match(style, $@"(?:^|;)\s*{property}: (?<value>[-\d.eE]+)px");
        Assert.True(match.Success, $"No {property} in `{style}`.");
        return double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
    }

    private static (double PanX, double PanY, double Scale) Transform(string style)
    {
        var match = Regex.Match(
            style,
            @"translate\((?<x>[-\d.eE]+)px, (?<y>[-\d.eE]+)px\) scale\((?<scale>[-\d.eE]+)\)"
        );
        Assert.True(match.Success, $"No transform in `{style}`.");
        return (
            double.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["scale"].Value, CultureInfo.InvariantCulture)
        );
    }

    private static string MapStyle(IRenderedComponent<Minimap> minimap) =>
        minimap.Find(".d12-minimap-content").GetAttribute("style")!;

    private static string RectStyle(IRenderedComponent<Minimap> minimap) =>
        minimap.Find(".d12-minimap-viewport").GetAttribute("style")!;

    private static string CanvasContentStyle(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find(".canvas-content").GetAttribute("style")!;

    private static void AssertPanNear(
        double expectedX,
        double expectedY,
        IRenderedComponent<DiagramCanvas> canvas
    )
    {
        Assert.Equal(expectedX, canvas.Instance.ZoomPanTracker.PanX, 6);
        Assert.Equal(expectedY, canvas.Instance.ZoomPanTracker.PanY, 6);
    }

    private static int BoxRenders(IRenderedComponent<Minimap> minimap) =>
        minimap.FindComponent<MinimapBoxes>().RenderCount;

    [Fact]
    public async Task ItShowsOneBoxPerInstanceInBoardUnitsAndTheViewportRectInMinimapPixels()
    {
        var minimap = await RenderMinimap(RenderCanvas(SeededBoard()));

        var boxes = minimap.FindAll(".d12-minimap-box");
        Assert.Equal(2, boxes.Count);
        Assert.Equal(
            "left: 100px; top: 100px; width: 50px; height: 50px",
            boxes[0].GetAttribute("style")
        );

        var map = Transform(MapStyle(minimap));
        Assert.Equal(0.225, map.Scale, 9);
        Assert.Equal(10, map.PanX, 9);
        Assert.Equal(7.5, map.PanY, 9);

        var rect = RectStyle(minimap);
        Assert.Equal(10, Px(rect, "left"), 9);
        Assert.Equal(7.5, Px(rect, "top"), 9);
        Assert.Equal(180, Px(rect, "width"), 9);
        Assert.Equal(135, Px(rect, "height"), 9);
    }

    [Fact]
    public async Task ItIsHiddenFromAssistiveTechnologyAndHasNoTabStop()
    {
        var minimap = await RenderMinimap(RenderCanvas(SeededBoard()));

        Assert.Equal("true", minimap.Find(".d12-minimap").GetAttribute("aria-hidden"));
        Assert.Empty(minimap.FindAll("[tabindex]"));
        Assert.Empty(minimap.FindAll("button, a, input, select, textarea"));
    }

    [Fact]
    public async Task NothingIsDrawnUntilTheMinimapHasBeenMeasured()
    {
        var minimap = await RenderMinimap(RenderCanvas(SeededBoard()), measured: false);

        Assert.Empty(minimap.FindAll(".d12-minimap-content"));
        Assert.Empty(minimap.FindAll(".d12-minimap-viewport"));
    }

    [Fact]
    public async Task PanningTheCanvasMovesTheRectAndRendersNoBox()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);
        var rendersBefore = BoxRenders(minimap);

        await canvas.InvokeAsync(() => canvas.Instance.ZoomPanTracker.SetPanPosition(-40, -20));

        var map = Transform(MapStyle(minimap));
        var rect = RectStyle(minimap);
        Assert.Equal(40 * map.Scale + map.PanX, Px(rect, "left"), 9);
        Assert.Equal(20 * map.Scale + map.PanY, Px(rect, "top"), 9);
        Assert.Equal(rendersBefore, BoxRenders(minimap));
    }

    [Fact]
    public async Task ZoomingTheCanvasResizesTheRect()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.ZoomPanTracker.Scale = 2);

        var map = Transform(MapStyle(minimap));
        Assert.Equal(400 * map.Scale, Px(RectStyle(minimap), "width"), 9);
    }

    [Fact]
    public async Task PlacingAShapeAddsABox()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);
        var rendersBefore = BoxRenders(minimap);

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        Assert.Equal(3, minimap.FindAll(".d12-minimap-box").Count);
        Assert.Equal(rendersBefore + 1, BoxRenders(minimap));
    }

    [Fact]
    public async Task UndoingThePlacementTakesTheBoxAway()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(2, minimap.FindAll(".d12-minimap-box").Count);
    }

    [Fact]
    public async Task ANewBoardOnTheCanvasRedrawsTheBoxes()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);
        var next = new Board();
        next.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(5000, 5000, 80, 40))
        );

        canvas.Render(parameters => parameters.Add(p => p.Board, next));

        var box = Assert.Single(minimap.FindAll(".d12-minimap-box"));
        Assert.Equal(
            "left: 5000px; top: 5000px; width: 80px; height: 40px",
            box.GetAttribute("style")
        );
    }

    // The new board frames to the same view as the old, so no viewport change tells the minimap.
    [Fact]
    public async Task ANewBoardThatLeavesTheViewWhereItWasStillRedrawsTheBoxes()
    {
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, SeededBoard()).Add(p => p.SnapToGrid, false)
        );
        var minimap = await RenderMinimap(canvas);
        var next = SeededBoard();
        next.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(250, 250, 50, 50))
        );

        canvas.Render(parameters => parameters.Add(p => p.Board, next));

        Assert.Equal(3, minimap.FindAll(".d12-minimap-box").Count);
    }

    [Fact]
    public async Task FarFromTheContentTheMapStillHoldsBothTheContentAndTheViewport()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);

        await canvas.InvokeAsync(
            () => canvas.Instance.ZoomPanTracker.SetPanPosition(-20000, -15000)
        );

        var map = Transform(MapStyle(minimap));
        var rect = RectStyle(minimap);
        Assert.InRange(Px(rect, "left"), 0, 200);
        Assert.InRange(Px(rect, "left") + Px(rect, "width"), 0, 200);
        Assert.InRange(Px(rect, "top") + Px(rect, "height"), 0, 150);
        Assert.InRange(100 * map.Scale + map.PanX, 0, 200);
        Assert.InRange(100 * map.Scale + map.PanY, 0, 150);
    }

    [Fact]
    public async Task AClickFliesTheViewportCentreToThePointAndKeepsTheScale()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);

        await minimap.Press(55, 41.25);
        await minimap.Release(55, 41.25);

        AssertPanNear(200, 150, canvas);
        Assert.Equal(1, canvas.Instance.ZoomPanTracker.Scale);
        Assert.Contains("transition: transform 250ms", CanvasContentStyle(canvas));
        Assert.Equal("true", canvas.Find(".canvas-content").GetAttribute("data-d12-flight"));
    }

    [Fact]
    public async Task ADragPansLiveWithNoEasingAndTheReleaseMovesNothingFurther()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);

        await minimap.Press(100, 75);
        await minimap.Move(55, 41.25);

        AssertPanNear(200, 150, canvas);
        Assert.DoesNotContain("transition", CanvasContentStyle(canvas));
        Assert.Null(canvas.Find(".canvas-content").GetAttribute("data-d12-flight"));

        await minimap.Release(55, 41.25);

        AssertPanNear(200, 150, canvas);
        Assert.DoesNotContain("transition", CanvasContentStyle(canvas));
    }

    [Fact]
    public async Task TheMapHoldsStillThroughADragAndSettlesAtRelease()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);
        var mapAtPress = MapStyle(minimap);

        await minimap.Press(100, 75);
        await minimap.Move(190, 140);

        Assert.Equal(mapAtPress, MapStyle(minimap));
        var rect = RectStyle(minimap);
        Assert.Equal(190, Px(rect, "left") + Px(rect, "width") / 2, 6);

        await minimap.Release(190, 140);

        Assert.NotEqual(mapAtPress, MapStyle(minimap));
    }

    [Fact]
    public async Task APressWhileACanvasGestureHoldsThePointerIsDropped()
    {
        var canvas = RenderCanvas(SeededBoard());
        var minimap = await RenderMinimap(canvas);
        await canvas.Press(10, 10, PointerPress.MiddleButton);

        await minimap.Press(100, 75);
        await minimap.Move(55, 41.25);
        await minimap.Release(55, 41.25);

        AssertPanNear(0, 0, canvas);
        await canvas.Release(10, 10, PointerPress.MiddleButton);
        AssertPanNear(0, 0, canvas);
    }

    [Fact]
    public async Task AMinimapClickLeavesTheSelectionAlone()
    {
        var board = SeededBoard();
        var canvas = RenderCanvas(board);
        var minimap = await RenderMinimap(canvas);
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        await minimap.Press(55, 41.25);
        await minimap.Release(55, 41.25);

        Assert.Equal(
            "true",
            canvas.FindAll(".component-container")[0].GetAttribute("aria-selected")
        );
    }

    [Fact]
    public async Task RemovingTheMinimapMidDragEndsThePressSoTheKeyboardWorksAgain()
    {
        var board = SeededBoard();
        var canvas = RenderCanvas(board);
        var minimap = await RenderMinimap(canvas);
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        await minimap.Press(100, 75);
        await minimap.Move(55, 41.25);

        await minimap.InvokeAsync(() => minimap.Instance.DisposeAsync().AsTask());
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.Single(board.Components);
        AssertPanNear(200, 150, canvas);
    }

    [Fact]
    public async Task ItsListenerTakesPressesUnclassifiedAndSendsFocusToTheCanvas()
    {
        var canvas = RenderCanvas(SeededBoard());
        await RenderMinimap(canvas);

        var registrations = CanvasModule.Invocations["addPointerListener"];
        Assert.Equal(2, registrations.Count);
        var options = registrations[1].Arguments[3]!;
        Assert.Equal(false, options.GetType().GetProperty("classify")!.GetValue(options));
        Assert.Equal(
            canvas.Instance.MinimapFocusTarget,
            options.GetType().GetProperty("focusTarget")!.GetValue(options)
        );
    }
}
