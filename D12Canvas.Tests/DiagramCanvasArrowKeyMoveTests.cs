using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Arrow-key move: a zoom-relative nudge of the focused/selected content, uniform for a
// single instance, an ad-hoc multi-selection, and a persisted Group. With nothing selected, arrow
// keys fall back to the pre-existing pan behaviour instead (matches DiagramCanvasUndoRedoTests's
// convention of invoking the JSInvokable handlers directly rather than through the real JS
// keydown/keyup listener, which SetupDiagramCanvasJsModule stubs out).
public class DiagramCanvasArrowKeyMoveTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasArrowKeyMoveTests()
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

    private static ComponentInstance AddInstance(Board board, double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Theory]
    [InlineData("ArrowLeft", -1, 0)]
    [InlineData("ArrowRight", 1, 0)]
    [InlineData("ArrowUp", 0, -1)]
    [InlineData("ArrowDown", 0, 1)]
    public async Task ArrowKeyNudgesTheSelectedInstanceByOneScreenPixelAtDefaultZoom(
        string code,
        double expectedDirX,
        double expectedDirY
    )
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed(code, false));

        Assert.Equal(new Bounds(100 + expectedDirX, 100 + expectedDirY, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task ShiftArrowNudgesByTenScreenPixels()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", true));

        Assert.Equal(new Bounds(110, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task TheStepShrinksAsTheCanvasZoomsIn()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ZoomIn(); // zooms to scale 1.1
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        // Computed with the same arithmetic ZoomPanTracker uses (1.0 + 0.1), rather than the
        // decimal literal 1.1, so this can't disagree with production code over double rounding.
        var scale = 1.0 + 0.1;
        Assert.Equal(100 + 1 / scale, instance.Bounds.X, precision: 10);
    }

    [Fact]
    public async Task TheStepGrowsAsTheCanvasZoomsOut()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ZoomOut(); // zooms to scale 0.9
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        var scale = 1.0 - 0.1;
        Assert.Equal(100 + 1 / scale, instance.Bounds.X, precision: 10);
    }

    [Fact]
    public async Task ANudgeIsUndoableAndRedoable()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.Equal(new Bounds(101, 100, 50, 50), instance.Bounds);
    }

    // Holding an arrow key fires many rapid repeat keydown events before the matching keyup - the
    // whole span must read as one undoable gesture, the same "record once on commit" discipline a
    // mouse drag's own press-to-release span already follows.
    [Fact]
    public async Task AHeldKeyBurstOfNudgesCoalescesIntoOneUndoableEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        Assert.Equal(new Bounds(103, 100, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.Equal(new Bounds(103, 100, 50, 50), instance.Bounds);
    }

    // The matching keyup (OnArrowKeyReleased) ends the current burst, so a later press starts a
    // fresh history entry instead of resuming the one that already ended.
    [Fact]
    public async Task ReleasingTheKeyEndsTheBurstSoTheNextPressIsASeparateHistoryEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyReleased());
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        Assert.Equal(new Bounds(102, 100, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(101, 100, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task AnAdHocMultiSelectionNudgesEveryMemberTogetherAsOneUndoableStep()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        Assert.Equal(new Bounds(101, 100, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(301, 100, 50, 50), second.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(100, 100, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(300, 100, 50, 50), second.Bounds);
    }

    [Fact]
    public async Task APersistedGroupNudgesEveryMemberTogetherAsOneUndoableStep()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowDown", false));

        Assert.Equal(new Bounds(100, 101, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(300, 101, 50, 50), second.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(100, 100, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(300, 100, 50, 50), second.Bounds);
    }

    [Fact]
    public async Task WithNothingSelectedArrowKeysPanTheCanvasInsteadOfNudging()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ReturnToOrigin();

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
        Assert.Equal(-50, canvas.Instance.ZoomPanTracker.PanX);
    }

    [Theory]
    [InlineData(0.1, 9)]
    [InlineData(4.0, 30)]
    public async Task ArrowPanMovesTheViewportByTheSameScreenDistanceAtAnyZoom(
        double expectedScale,
        int zoomSteps
    )
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        for (var i = 0; i < zoomSteps; i++)
        {
            await canvas.InvokeAsync(
                expectedScale < 1 ? canvas.Instance.OnZoomOut : canvas.Instance.OnZoomIn
            );
        }
        var tracker = canvas.Instance.ZoomPanTracker;
        Assert.Equal(expectedScale, tracker.Scale, precision: 6);
        var viewportBefore = tracker.Viewport;

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        var screenDistance = (tracker.Viewport.X - viewportBefore.X) * tracker.Scale;
        Assert.Equal(50, screenDistance, precision: 6);
    }

    private IRenderedComponent<DiagramCanvas> RenderWithSnap(Board board) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, true)
        );

    [Theory]
    [InlineData("ArrowRight", 120, 93)]
    [InlineData("ArrowLeft", 100, 93)]
    [InlineData("ArrowDown", 107, 100)]
    [InlineData("ArrowUp", 107, 80)]
    public async Task WithSnapOnANudgeFromOffGridLandsOnTheNextGridLineInTheArrowsDirection(
        string code,
        double expectedX,
        double expectedY
    )
    {
        var board = new Board();
        var instance = AddInstance(board, 107, 93);
        var canvas = RenderWithSnap(board);
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed(code, false));

        Assert.Equal(new Bounds(expectedX, expectedY, 50, 50), instance.Bounds);
    }

    [Theory]
    [InlineData("ArrowRight", 120, 100)]
    [InlineData("ArrowLeft", 80, 100)]
    public async Task WithSnapOnANudgeFromAGridLineMovesExactlyOneLine(
        string code,
        double expectedX,
        double expectedY
    )
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderWithSnap(board);
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed(code, false));

        Assert.Equal(new Bounds(expectedX, expectedY, 50, 50), instance.Bounds);
    }

    [Theory]
    [InlineData("ArrowRight", 300)]
    [InlineData("ArrowLeft", -80)]
    public async Task WithSnapOnShiftNudgeLandsOnTheTenthGridLine(string code, double expectedX)
    {
        var board = new Board();
        var instance = AddInstance(board, 107, 100);
        var canvas = RenderWithSnap(board);
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed(code, true));

        Assert.Equal(new Bounds(expectedX, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task WithSnapOnTheGridStepFollowsTheDominantLayerAsTheCanvasZoomsOut()
    {
        var board = new Board();
        var instance = AddInstance(board, 107, 100);
        var canvas = RenderWithSnap(board);
        canvas.ClickOn(canvas.Find(".component-container"));
        for (var i = 0; i < 9; i++)
        {
            await canvas.InvokeAsync(canvas.Instance.OnZoomOut);
        }

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        Assert.Equal(new Bounds(200, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task WithSnapOnAHeldBurstWalksGridLinesAndUndoesInOneStep()
    {
        var board = new Board();
        var instance = AddInstance(board, 107, 100);
        var canvas = RenderWithSnap(board);
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        Assert.Equal(new Bounds(160, 100, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(107, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task WithSnapOnAMultiSelectionNudgesFromTheTopLeftOfItsBoundingBox()
    {
        var board = new Board();
        var first = AddInstance(board, 107, 100);
        var second = AddInstance(board, 213, 53);
        var canvas = RenderWithSnap(board);
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowUp", false));

        Assert.Equal(new Bounds(120, 87, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(226, 40, 50, 50), second.Bounds);
    }

    private IRenderedComponent<DiagramCanvas> RenderFree(Board board)
    {
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ReturnToOrigin();
        return canvas;
    }

    [Fact]
    public async Task ASelectedEdgesFloatingEndsMoveWithTheNudge()
    {
        var board = new Board();
        var edge = new Edge(new FloatingEndpoint(100, 300), new FloatingEndpoint(200, 300));
        board.AddEdge(edge);
        var canvas = RenderFree(board);
        canvas.ClickElement(canvas.Find(".edge-hit"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", true));

        Assert.Equal(new FloatingEndpoint(110, 300), edge.Source);
        Assert.Equal(new FloatingEndpoint(210, 300), edge.Target);
        Assert.Equal(0, canvas.Instance.ZoomPanTracker.PanX);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new FloatingEndpoint(100, 300), edge.Source);
        Assert.Equal(new FloatingEndpoint(200, 300), edge.Target);
    }

    [Fact]
    public async Task AMixedSelectionNudgesItsInstancesAndFloatingEndsAsOneEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var edge = new Edge(
            new PortEndpoint(instance.Id, PortId.Right),
            new FloatingEndpoint(300, 125)
        );
        board.AddEdge(edge);
        var canvas = RenderFree(board);
        canvas.ClickOn(canvas.Find(".component-container"));
        canvas.ClickElement(canvas.Find(".edge-hit"), shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowDown", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowDown", false));

        Assert.Equal(new Bounds(100, 102, 50, 50), instance.Bounds);
        Assert.Equal(new PortEndpoint(instance.Id, PortId.Right), edge.Source);
        Assert.Equal(new FloatingEndpoint(300, 127), edge.Target);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
        Assert.Equal(new FloatingEndpoint(300, 125), edge.Target);
    }

    [Fact]
    public async Task WithSnapAnEdgeOnlySelectionNudgesFromItsFloatingEndsToTheNextGridLine()
    {
        var board = new Board();
        var edge = new Edge(new FloatingEndpoint(107, 300), new FloatingEndpoint(213, 290));
        board.AddEdge(edge);
        var canvas = RenderWithSnap(board);
        canvas.ClickElement(canvas.Find(".edge-hit"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        Assert.Equal(new FloatingEndpoint(120, 300), edge.Source);
        Assert.Equal(new FloatingEndpoint(226, 290), edge.Target);
    }

    // An edge-only selection takes the nudge even when it has nothing to move, so one key never
    // means "nudge" for one edge and "pan" for another.
    [Fact]
    public async Task WithOnlyAnAttachedEdgeSelectedArrowKeysNeitherPanNorWrite()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        var target = AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ReturnToOrigin();
        canvas.PressPort(source.Id, "Right", (150, 125));
        canvas.MoveTo((250, 125));
        canvas.ReleaseOverPort((250, 125), target.Id, "Left");
        canvas.ClickElement(canvas.Find(".edge-hit"));

        var panX = canvas.Instance.ZoomPanTracker.PanX;

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        Assert.Equal(panX, canvas.Instance.ZoomPanTracker.PanX);
        Assert.Equal(new Bounds(100, 100, 50, 50), source.Bounds);
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Empty(board.Edges);
    }
}
