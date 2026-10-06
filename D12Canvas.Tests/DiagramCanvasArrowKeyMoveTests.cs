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
        SetupComponentContainerJsModule();

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed(code, false));

        Assert.Equal(new Bounds(100 + expectedDirX, 100 + expectedDirY, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task ShiftArrowNudgesByTenScreenPixels()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", true));

        Assert.Equal(new Bounds(110, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task TheStepShrinksAsTheCanvasZoomsIn()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
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

    // An edge's own selection slot (_selectedEdgeId) is never mixed into the instance selection
    // arrow-key nudge reads (ExpandedSelection) - a selected edge has no Bounds of its own to
    // nudge, so this must fall back to panning too, not silently no-op.
    [Fact]
    public async Task WithOnlyAnEdgeSelectedArrowKeysPanTheCanvasInsteadOfNudging()
    {
        var board = new Board();
        AddInstance(board, 100, 100); // right port at (150, 125)
        AddInstance(board, 250, 100); // left port at (250, 125)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        canvas.PressElement(containers[0].QuerySelector(".port-right")!, (150, 125));
        var targetPort = containers[1].QuerySelector(".port-left")!;
        canvas.MoveTo((250, 125));
        canvas.ReleaseOver((250, 125), targetPort);
        canvas.ClickElement(canvas.Find(".edge-hit"));

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        Assert.Equal(-50, canvas.Instance.ZoomPanTracker.PanX);
    }
}
