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
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed(code, false));

        Assert.Equal(new Bounds(100 + expectedDirX, 100 + expectedDirY, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task ShiftArrowNudgesByTenScreenPixels()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", true));

        Assert.Equal(new Bounds(110, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task TheStepShrinksAsTheCanvasZoomsIn()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 }); // zooms to scale 1.1
        canvas.Find(".component-container").Click();

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
        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = 100 }); // zooms to scale 0.9
        canvas.Find(".component-container").Click();

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
        canvas.Find(".component-container").Click();
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
        canvas.Find(".component-container").Click();

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
        canvas.Find(".component-container").Click();

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
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

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
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
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
        containers[0]
            .QuerySelector(".port-right")!
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        var targetPort = containers[1].QuerySelector(".port-left")!;
        targetPort.MouseMove(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        targetPort.MouseUp(new MouseEventArgs { ClientX = 250, ClientY = 125 });
        canvas.Find(".edge-line").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        Assert.Equal(-50, canvas.Instance.ZoomPanTracker.PanX);
    }
}
