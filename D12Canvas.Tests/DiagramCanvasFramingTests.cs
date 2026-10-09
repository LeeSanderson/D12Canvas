using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The canvas opens a new Board framed on its content, unanimated, and Zoom to Fit, Zoom to
// Selection and Zoom to 100% each move the viewport in one change that the content element
// animates as a framing flight. The container is 800 by 600.
public class DiagramCanvasFramingTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";
    private const string FlightTransition = "transition: transform 250ms";

    public DiagramCanvasFramingTests()
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
                DefaultSize: null,
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    private static ComponentInstance AddInstance(Board board, Bounds bounds)
    {
        var instance = new ComponentInstance(ComponentTypeKey, new TestProps(), bounds);
        board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

    private static ZoomPanTracker Tracker(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Instance.ZoomPanTracker;

    private static (double X, double Y) OnScreen(
        IRenderedComponent<DiagramCanvas> canvas,
        double boardX,
        double boardY
    )
    {
        var tracker = Tracker(canvas);
        return (boardX * tracker.Scale + tracker.PanX, boardY * tracker.Scale + tracker.PanY);
    }

    private static void AssertCentredOnScreen(IRenderedComponent<DiagramCanvas> canvas, Bounds box)
    {
        var centre = OnScreen(canvas, box.X + box.Width / 2, box.Y + box.Height / 2);
        Assert.Equal(400, centre.X, precision: 9);
        Assert.Equal(300, centre.Y, precision: 9);
    }

    private static IElement Content(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find(".canvas-content");

    private static void AssertInFlight(IRenderedComponent<DiagramCanvas> canvas)
    {
        Assert.Contains(FlightTransition, Content(canvas).GetAttribute("style"));
        Assert.Equal("true", Content(canvas).GetAttribute("data-d12-flight"));
    }

    private static void AssertNotInFlight(IRenderedComponent<DiagramCanvas> canvas)
    {
        Assert.DoesNotContain("transition", Content(canvas).GetAttribute("style"));
        Assert.Null(Content(canvas).GetAttribute("data-d12-flight"));
    }

    private static void SetView(
        IRenderedComponent<DiagramCanvas> canvas,
        double scale,
        double panX,
        double panY
    ) =>
        canvas
            .InvokeAsync(() =>
            {
                Tracker(canvas).SetScaleAbout(0, 0, scale);
                Tracker(canvas).SetPanPosition(panX, panY);
            })
            .GetAwaiter()
            .GetResult();

    private static List<ZoomPanChangedEventArgs> RecordChanges(
        IRenderedComponent<DiagramCanvas> canvas
    )
    {
        var changes = new List<ZoomPanChangedEventArgs>();
        canvas.Instance.ZoomOrPanChanged += (_, args) => changes.Add(args);
        return changes;
    }

    private static void Run(IRenderedComponent<DiagramCanvas> canvas, Action command) =>
        canvas.InvokeAsync(command).GetAwaiter().GetResult();

    private static IElement Row(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas
            .FindAll(".d12-context-menu-item")
            .Single(item => item.QuerySelector(".d12-context-menu-label")!.TextContent == label);

    [Fact]
    public void ABoardAuthoredFarFromTheOriginOpensFramedOnItsContentWithNoFlight()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(50000, 50000, 100, 100));

        var canvas = RenderCanvas(board);

        Assert.Equal(1.0, Tracker(canvas).Scale);
        AssertCentredOnScreen(canvas, shape.Bounds);
        Assert.Single(canvas.FindAll(".component-container"));
        AssertNotInFlight(canvas);
    }

    [Fact]
    public void ABoardLargerThanTheContainerOpensZoomedOutToFitWithinTheMargin()
    {
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 100, 100));
        AddInstance(board, new Bounds(3900, 2900, 100, 100));

        var canvas = RenderCanvas(board);

        Assert.Equal(0.9 * 600 / 3000, Tracker(canvas).Scale, precision: 9);
        AssertCentredOnScreen(canvas, new Bounds(0, 0, 4000, 3000));
    }

    [Fact]
    public void AnEmptyBoardOpensAtScaleOneAndPanOrigin()
    {
        var canvas = RenderCanvas(new Board());

        Assert.Equal(
            (1.0, 0.0, 0.0),
            (Tracker(canvas).Scale, Tracker(canvas).PanX, Tracker(canvas).PanY)
        );
    }

    [Fact]
    public void ABoardHoldingOnlyAFloatingEdgeOpensFramedOnThatEdge()
    {
        var board = new Board();
        board.AddEdge(new Edge(new FloatingEndpoint(9000, 700), new FloatingEndpoint(9200, 900)));

        var canvas = RenderCanvas(board);

        AssertCentredOnScreen(canvas, new Bounds(9000, 700, 200, 200));
    }

    [Fact]
    public async Task ABoardMountedInAnUnmeasuredContainerIsFittedWhenTheContainerFirstHasASize()
    {
        CanvasModule
            .Setup<InitialFacts>("initialFacts", _ => true)
            .SetResult(new InitialFacts(0, 0, false, false));
        var board = new Board();
        var shape = AddInstance(board, new Bounds(-9000, 4000, 100, 100));
        var canvas = RenderCanvas(board);
        Assert.Empty(canvas.FindAll(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnContainerResized(800, 600));

        AssertCentredOnScreen(canvas, shape.Bounds);
        Assert.Single(canvas.FindAll(".component-container"));
    }

    [Fact]
    public void TheInitialFitRunsOnceForABoardAndAgainForANewBoard()
    {
        var board = new Board();
        AddInstance(board, new Bounds(2000, 2000, 100, 100));
        var canvas = RenderCanvas(board);
        SetView(canvas, 1.0, 35, -20);

        canvas.Render(parameters => parameters.Add(p => p.ObjectSnapping, true));

        Assert.Equal((35.0, -20.0), (Tracker(canvas).PanX, Tracker(canvas).PanY));

        var next = new Board();
        var shape = AddInstance(next, new Bounds(-7000, 300, 100, 100));
        canvas.Render(parameters => parameters.Add(p => p.Board, next));

        AssertCentredOnScreen(canvas, shape.Bounds);
    }

    [Fact]
    public void ZoomToFitFramesAllContentInOneChangeAsAFlight()
    {
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 100, 100));
        AddInstance(board, new Bounds(900, 500, 100, 100));
        var canvas = RenderCanvas(board);
        SetView(canvas, 2.5, -4000, 1300);
        var changes = RecordChanges(canvas);

        Run(canvas, canvas.Instance.ZoomToFit);

        Assert.Single(changes);
        Assert.Equal(0.72, Tracker(canvas).Scale, precision: 9);
        AssertCentredOnScreen(canvas, new Bounds(0, 0, 1000, 600));
        AssertInFlight(canvas);
    }

    [Fact]
    public void ZoomToFitOnAnEmptyBoardChangesNothing()
    {
        var canvas = RenderCanvas(new Board());
        SetView(canvas, 0.5, 10, 20);
        var changes = RecordChanges(canvas);

        Run(canvas, canvas.Instance.ZoomToFit);

        Assert.Empty(changes);
        AssertNotInFlight(canvas);
    }

    [Fact]
    public void ZoomToSelectionFramesOnlyTheSelection()
    {
        var board = new Board();
        var selected = AddInstance(board, new Bounds(0, 0, 100, 100));
        AddInstance(board, new Bounds(6000, 6000, 100, 100));
        var canvas = RenderCanvas(board);
        canvas.ReturnToOrigin();
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        Run(canvas, canvas.Instance.ZoomToSelection);

        Assert.Equal(1.0, Tracker(canvas).Scale);
        AssertCentredOnScreen(canvas, selected.Bounds);
        AssertInFlight(canvas);
    }

    [Fact]
    public void ZoomToSelectionWithNothingSelectedChangesNothingAndNeverFramesEverything()
    {
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 100, 100));
        AddInstance(board, new Bounds(6000, 6000, 100, 100));
        var canvas = RenderCanvas(board);
        SetView(canvas, 1.0, 0, 0);
        var changes = RecordChanges(canvas);

        Run(canvas, canvas.Instance.ZoomToSelection);

        Assert.Empty(changes);
    }

    [Fact]
    public async Task ZoomToSelectionFramesASelectedEdgeByItsEnds()
    {
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 100, 100));
        var edge = new Edge(new FloatingEndpoint(5000, 100), new FloatingEndpoint(5400, 300));
        board.AddEdge(edge);
        var canvas = RenderCanvas(board);
        canvas.ReturnToOrigin();
        await canvas.Press(10, 10, role: HitRole.Edge, entityId: edge.Id);
        await canvas.Release(10, 10);

        Run(canvas, canvas.Instance.ZoomToSelection);

        AssertCentredOnScreen(canvas, new Bounds(5000, 100, 400, 200));
    }

    [Fact]
    public void ZoomTo100PercentKeepsTheBoardPointAtTheCentreOfTheContainer()
    {
        var board = new Board();
        AddInstance(board, new Bounds(0, 0, 100, 100));
        var canvas = RenderCanvas(board);
        SetView(canvas, 0.25, 130, -70);
        var centreBefore = ((400 - 130) / 0.25, (300 + 70) / 0.25);
        var changes = RecordChanges(canvas);

        Run(canvas, canvas.Instance.ZoomTo100Percent);

        Assert.Single(changes);
        Assert.Equal(1.0, Tracker(canvas).Scale);
        var centre = OnScreen(canvas, centreBefore.Item1, centreBefore.Item2);
        Assert.Equal(400, centre.X, precision: 9);
        Assert.Equal(300, centre.Y, precision: 9);
        AssertInFlight(canvas);
    }

    [Fact]
    public async Task TheNextViewportChangeThatIsNotFramingEndsTheFlightsTransition()
    {
        var board = new Board();
        AddInstance(board, new Bounds(3000, 0, 100, 100));
        var canvas = RenderCanvas(board);
        canvas.ReturnToOrigin();
        Run(canvas, canvas.Instance.ZoomToFit);

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        AssertNotInFlight(canvas);
    }

    [Fact]
    public void AFlightStartedMidDragKeepsTheDragGoingAndTheShapeUnderThePointer()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(100, 100, 50, 50));
        AddInstance(board, new Bounds(2000, 1500, 50, 50));
        var canvas = RenderCanvas(board);
        canvas.ReturnToOrigin();

        canvas.PressOn(canvas.FindAll(".component-container")[0], (120, 120));
        canvas.MoveTo((200, 200));
        Run(canvas, canvas.Instance.ZoomToFit);
        canvas.ReleaseAt((200, 200));

        var tracker = Tracker(canvas);
        Assert.NotEqual(1.0, tracker.Scale);
        var underPointer = (
            (200 - tracker.PanX) / tracker.Scale,
            (200 - tracker.PanY) / tracker.Scale
        );
        Assert.Equal(underPointer.Item1 - 20, shape.Bounds.X, precision: 8);
        Assert.Equal(underPointer.Item2 - 20, shape.Bounds.Y, precision: 8);
    }

    [Fact]
    public async Task AHostCompensatesForFloatedChromeByPanningTheTrackerAfterTheFit()
    {
        var board = new Board();
        AddInstance(board, new Bounds(50000, 50000, 100, 100));
        var canvas = RenderCanvas(board);
        var fitted = (Tracker(canvas).PanX, Tracker(canvas).PanY);

        await canvas.InvokeAsync(() => Tracker(canvas).Pan(150, 0));

        Assert.Contains(
            $"translate({fitted.PanX + 150}px, {fitted.PanY}px)",
            Content(canvas).GetAttribute("style")
        );
    }

    [Fact]
    public async Task TheCanvasMenusZoomToFitAndTo100PercentRowsRunTheirCommands()
    {
        var board = new Board();
        var shape = AddInstance(board, new Bounds(5000, 5000, 100, 100));
        var canvas = RenderCanvas(board);
        SetView(canvas, 0.5, 0, 0);

        await canvas.ClickCanvas(400, 400, PointerPress.SecondaryButton);
        Row(canvas, "Zoom to 100%").Click();

        Assert.Equal(1.0, Tracker(canvas).Scale);

        await canvas.ClickCanvas(400, 400, PointerPress.SecondaryButton);
        Row(canvas, "Zoom to Fit").Click();

        AssertCentredOnScreen(canvas, shape.Bounds);
        AssertInFlight(canvas);
    }

    [Fact]
    public async Task TheObjectMenusZoomToSelectionRowFramesTheSelection()
    {
        var board = new Board();
        var pressed = AddInstance(board, new Bounds(0, 0, 100, 100));
        AddInstance(board, new Bounds(6000, 0, 100, 100));
        var canvas = RenderCanvas(board);
        canvas.ReturnToOrigin();

        await canvas.Press(
            10,
            10,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: pressed.Id
        );
        await canvas.Release(10, 10, PointerPress.SecondaryButton);
        Row(canvas, "Zoom to Selection").Click();

        AssertCentredOnScreen(canvas, pressed.Bounds);
    }
}
