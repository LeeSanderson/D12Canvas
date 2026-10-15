using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Press-to-select-and-drag. A press on an instance selects it and a drag past the threshold moves
// the selection in the same motion. What a drag shows each frame comes from the gesture preview,
// Board is written once at release with exactly what was shown, and a drag is one undo step.
public class DiagramCanvasDragMoveTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasDragMoveTests()
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

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board, bool snapToGrid = false) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, snapToGrid)
        );

    private static string ContainerStyle(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']").GetAttribute("style")!;

    private static string? AriaSelected(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']").GetAttribute("aria-selected");

    private static IElement Container(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']");

    [Fact]
    public void AColdPressOnAnUnselectedInstanceSelectsAndDragsItInOneMotion()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board);

        canvas.DragOn(Container(canvas, instance.Id), (300, 200), (340, 175));

        Assert.Equal(new Bounds(140, 75, 50, 50), instance.Bounds);
        Assert.Equal("true", AriaSelected(canvas, instance.Id));
    }

    [Fact]
    public void DraggingScalesTheScreenDeltaByTheCurrentZoom()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board);

        canvas.ZoomIn();
        canvas.DragOn(Container(canvas, instance.Id), (300, 200), (344, 222));

        // Computed with the same arithmetic ZoomPanTracker uses (1.0 + 0.1), rather than the
        // decimal literal 1.1, so this can't disagree with production code over double rounding.
        var scale = 1.0 + 0.1;
        Assert.Equal(100 + 44 / scale, instance.Bounds.X, precision: 10);
        Assert.Equal(100 + 22 / scale, instance.Bounds.Y, precision: 10);
    }

    [Fact]
    public void TheBoardIsUnchangedMidDragWhileTheInstanceIsDrawnWhereThePreviewSays()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board);

        canvas.PressOn(Container(canvas, instance.Id), (300, 200));
        canvas.MoveTo((350, 260));

        Assert.Contains("left: 150px", ContainerStyle(canvas, instance.Id));
        Assert.Contains("top: 160px", ContainerStyle(canvas, instance.Id));
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);

        canvas.ReleaseAt((350, 260));

        Assert.Equal(new Bounds(150, 160, 50, 50), instance.Bounds);
    }

    [Fact]
    public void PressingAMemberOfAMultiSelectionMovesTheWholeSelectionAndKeepsIt()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(Container(canvas, first.Id));
        canvas.ClickOn(Container(canvas, second.Id), shift: true);

        canvas.DragOn(Container(canvas, first.Id), (120, 120), (150, 140));

        Assert.Equal(new Bounds(130, 120, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(330, 120, 50, 50), second.Bounds);
        Assert.Equal("true", AriaSelected(canvas, first.Id));
        Assert.Equal("true", AriaSelected(canvas, second.Id));
    }

    [Fact]
    public void AClickOnAMemberOfAMultiSelectionCollapsesTheSelectionToItAtRelease()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(Container(canvas, first.Id));
        canvas.ClickOn(Container(canvas, second.Id), shift: true);

        canvas.PressOn(Container(canvas, first.Id), (120, 120));
        Assert.Equal("true", AriaSelected(canvas, second.Id));

        canvas.ReleaseAt((120, 120));

        Assert.Equal("true", AriaSelected(canvas, first.Id));
        Assert.Null(AriaSelected(canvas, second.Id));
    }

    [Fact]
    public void AnAttachedEdgeFollowsTheShapeThroughoutTheDrag()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 400, 100);
        board.AddEdge(
            new Edge(
                new PortEndpoint(source.Id, PortId.Right),
                new PortEndpoint(target.Id, PortId.Left)
            )
        );
        var canvas = RenderCanvas(board);

        canvas.PressOn(Container(canvas, source.Id), (120, 120));
        canvas.MoveTo((150, 170));

        var line = canvas.Find(".edge-line");
        Assert.Equal("180", line.GetAttribute("x1"));
        Assert.Equal("175", line.GetAttribute("y1"));
    }

    [Fact]
    public void SnappingIsVisibleDuringTheDragAndNothingJumpsAtRelease()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board, snapToGrid: true);

        canvas.PressOn(Container(canvas, instance.Id), (120, 120));
        canvas.MoveTo((133, 147));

        Assert.Contains("left: 120px", ContainerStyle(canvas, instance.Id));
        Assert.Contains("top: 120px", ContainerStyle(canvas, instance.Id));

        canvas.ReleaseAt((133, 147));

        Assert.Equal(new Bounds(120, 120, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task ADragBackToItsStartingPointLeavesNoHistoryEntry()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var other = AddInstance(board, 300, 300);
        var canvas = RenderCanvas(board);
        canvas.DragOn(Container(canvas, other.Id), (310, 310), (330, 310));

        canvas.PressOn(Container(canvas, instance.Id), (120, 120));
        canvas.MoveTo((200, 200));
        canvas.MoveTo((120, 120));
        canvas.ReleaseAt((120, 120));
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
        Assert.Equal(new Bounds(300, 300, 50, 50), other.Bounds);
    }

    [Fact]
    public async Task OneDragOfAMultiSelectionIsOneUndoStep()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(Container(canvas, first.Id));
        canvas.ClickOn(Container(canvas, second.Id), shift: true);

        canvas.PressOn(Container(canvas, first.Id), (120, 120));
        canvas.MoveTo((130, 130));
        canvas.MoveTo((170, 150));
        canvas.ReleaseAt((170, 150));
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(300, 100, 50, 50), second.Bounds);
    }

    [Fact]
    public async Task AReleaseFarOutsideTheCanvasCommitsTheMoveAndUndoThenMovesItBack()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board);

        canvas.PressOn(Container(canvas, instance.Id), (120, 120));
        canvas.MoveTo((-500, -400));
        canvas.ReleaseAt((-500, -400));

        Assert.Equal(new Bounds(-520, -420, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Single(board.Components);
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task APressAndReleaseWithNoMovementDoesNotMutateBounds()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = RenderCanvas(board);

        canvas.ClickOn(Container(canvas, instance.Id));
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
        Assert.Single(board.Components);
    }

    [Fact]
    public async Task EscapeMidDragDropsThePreviewAndRestoresTheSelection()
    {
        var board = new Board();
        var pressed = AddInstance(board, 100, 100);
        var other = AddInstance(board, 300, 100);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(Container(canvas, other.Id));

        canvas.PressOn(Container(canvas, pressed.Id), (120, 120));
        canvas.MoveTo((200, 200));
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        Assert.Contains("left: 100px", ContainerStyle(canvas, pressed.Id));
        Assert.Equal("true", AriaSelected(canvas, other.Id));
        Assert.Null(AriaSelected(canvas, pressed.Id));

        canvas.ReleaseAt((200, 200));

        Assert.Equal(new Bounds(100, 100, 50, 50), pressed.Bounds);
    }

    // The second instance joins the selection while a pan has it on screen, and the pan back
    // leaves it outside the viewport, so the drag starts with one participant unmounted.
    [Fact]
    public async Task AShapeDraggedInFromOffScreenMountsAndStaysMountedUntilRelease()
    {
        var board = new Board();
        var onScreen = AddInstance(board, 100, 100);
        var offScreen = AddInstance(board, 900, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.Overscan, 0)
        );
        canvas.ReturnToOrigin();
        await canvas.InvokeAsync(() => canvas.Instance.OnContainerResized(800, 600));
        canvas.ClickOn(Container(canvas, onScreen.Id));
        await canvas.Pan(from: (600, 300), to: (100, 300));
        canvas.ClickOn(Container(canvas, offScreen.Id), shift: true);
        await canvas.Pan(from: (100, 300), to: (600, 300));
        Assert.Single(canvas.FindAll(".component-container"));

        canvas.PressOn(Container(canvas, onScreen.Id), (120, 120));
        canvas.MoveTo((-380, 120));
        Assert.Equal(2, canvas.FindAll(".component-container").Count);

        canvas.MoveTo((120, 120));
        Assert.Equal(2, canvas.FindAll(".component-container").Count);

        canvas.ReleaseAt((120, 120));
        Assert.Single(canvas.FindAll(".component-container"));
    }

    [Fact]
    public async Task AShapeCarriedPastTheViewportEdgeStaysMountedUntilRelease()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.Overscan, 0)
        );
        await canvas.InvokeAsync(() => canvas.Instance.OnContainerResized(800, 600));

        canvas.PressOn(Container(canvas, instance.Id), (120, 120));
        canvas.MoveTo((3000, 120));

        Assert.Single(canvas.FindAll(".component-container"));

        canvas.ReleaseAt((3000, 120));

        Assert.Empty(canvas.FindAll(".component-container"));
    }
}
