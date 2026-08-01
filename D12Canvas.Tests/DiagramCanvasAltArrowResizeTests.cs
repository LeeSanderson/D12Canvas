using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Alt+Arrow resize: a zoom-relative grow/shrink of the single selected instance -
// single-instance only (an ad-hoc multi-selection or a persisted Group has no keyboard
// resize). Matches DiagramCanvasArrowKeyMoveTests's convention of invoking the JSInvokable handlers
// directly rather than through the real JS keydown/keyup listener.
public class DiagramCanvasAltArrowResizeTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasAltArrowResizeTests()
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

    private static ComponentInstance AddInstance(
        Board board,
        double x,
        double y,
        double width = 200,
        double height = 150
    )
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, width, height)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Theory]
    [InlineData("ArrowRight", 201, 150, 100)] // right edge grows outward, left edge anchored
    [InlineData("ArrowDown", 200, 151, 100)] // bottom edge grows outward, top edge anchored
    public async Task PlainAltArrowGrowsTheEdgeMatchingItsDirection(
        string code,
        double expectedWidth,
        double expectedHeight,
        double expectedX
    )
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed(code, false));

        Assert.Equal(expectedWidth, instance.Bounds.Width);
        Assert.Equal(expectedHeight, instance.Bounds.Height);
        Assert.Equal(expectedX, instance.Bounds.X);
        Assert.Equal(100, instance.Bounds.Y);
    }

    [Fact]
    public async Task PlainAltArrowLeftGrowsWidthAnchoringTheRightEdge()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowLeft", false));

        Assert.Equal(99, instance.Bounds.X);
        Assert.Equal(201, instance.Bounds.Width);
        Assert.Equal(300, instance.Bounds.Right);
    }

    [Fact]
    public async Task PlainAltArrowUpGrowsHeightAnchoringTheBottomEdge()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowUp", false));

        Assert.Equal(99, instance.Bounds.Y);
        Assert.Equal(151, instance.Bounds.Height);
        Assert.Equal(250, instance.Bounds.Bottom);
    }

    [Fact]
    public async Task AltShiftArrowRightShrinksWidthAnchoringTheRightEdge()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", true));

        Assert.Equal(101, instance.Bounds.X);
        Assert.Equal(199, instance.Bounds.Width);
        Assert.Equal(300, instance.Bounds.Right);
    }

    [Fact]
    public async Task AltShiftArrowLeftShrinksWidthAnchoringTheLeftEdge()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowLeft", true));

        Assert.Equal(100, instance.Bounds.X);
        Assert.Equal(199, instance.Bounds.Width);
    }

    [Fact]
    public async Task AltShiftArrowDownShrinksHeightAnchoringTheBottomEdge()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowDown", true));

        Assert.Equal(101, instance.Bounds.Y);
        Assert.Equal(149, instance.Bounds.Height);
        Assert.Equal(250, instance.Bounds.Bottom);
    }

    [Fact]
    public async Task AltShiftArrowUpShrinksHeightAnchoringTheTopEdge()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowUp", true));

        Assert.Equal(100, instance.Bounds.Y);
        Assert.Equal(149, instance.Bounds.Height);
    }

    [Fact]
    public async Task TheStepShrinksAsTheCanvasZoomsIn()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 }); // zooms to scale 1.1
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));

        var scale = 1.0 + 0.1;
        Assert.Equal(200 + 1 / scale, instance.Bounds.Width, precision: 10);
    }

    [Fact]
    public async Task AResizeIsUndoableAndRedoable()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();
        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(100, 100, 200, 150), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.Equal(new Bounds(100, 100, 201, 150), instance.Bounds);
    }

    // Holding Alt+Arrow fires many rapid repeat keydowns before the matching keyup - the whole span
    // must read as one undoable gesture, same discipline as the plain arrow-key nudge.
    [Fact]
    public async Task AHeldKeyBurstOfResizeStepsCoalescesIntoOneUndoableEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));
        Assert.Equal(203, instance.Bounds.Width);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(100, 100, 200, 150), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.Equal(203, instance.Bounds.Width);
    }

    [Fact]
    public async Task ReleasingTheKeyEndsTheBurstSoTheNextPressIsASeparateHistoryEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyReleased());
        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));
        Assert.Equal(202, instance.Bounds.Width);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(201, instance.Bounds.Width);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(200, instance.Bounds.Width);
    }

    // A direction change mid-burst (Shift toggling, which flips the anchor) must not silently
    // extend the previous command - that would resize relative to the wrong anchor edge.
    [Fact]
    public async Task ToggingShiftMidBurstStartsANewHistoryEntryInsteadOfExtendingTheOldAnchor()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", true));

        // Grow-by-1 (left-anchored) then shrink-by-1 (right-anchored, from the same starting
        // bounds each has its own anchor for) nets back to the original width.
        Assert.Equal(200, instance.Bounds.Width);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(201, instance.Bounds.Width);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(200, instance.Bounds.Width);
    }

    [Fact]
    public async Task ShrinkingNeverGoesBelowTheMinimumSize()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        for (var i = 0; i < 10; i++)
        {
            await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowLeft", true));
        }

        Assert.Equal(50, instance.Bounds.Width);
        Assert.True(instance.Bounds.Width > 0);
    }

    [Fact]
    public async Task WithNothingSelectedAltArrowDoesNothing()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));

        Assert.Equal(new Bounds(100, 100, 200, 150), instance.Bounds);
    }

    [Fact]
    public async Task WithAMultiSelectionAltArrowDoesNothing()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100, 200, 150);
        var second = AddInstance(board, 400, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));

        Assert.Equal(new Bounds(100, 100, 200, 150), first.Bounds);
        Assert.Equal(new Bounds(400, 100, 200, 150), second.Bounds);
    }

    [Fact]
    public async Task WithAGroupSelectedAltArrowDoesNothing()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100, 200, 150);
        var second = AddInstance(board, 400, 100, 200, 150);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));

        Assert.Equal(new Bounds(100, 100, 200, 150), first.Bounds);
        Assert.Equal(new Bounds(400, 100, 200, 150), second.Bounds);
    }
}
