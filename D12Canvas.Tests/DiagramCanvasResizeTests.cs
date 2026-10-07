using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Resize a selected instance via its handles, driven through the pointer entry points. A resize
// is one press-to-release gesture: the shape follows the pointer through the gesture preview,
// Board only sees the final bounds on release, and one resize is one undo step.
public class DiagramCanvasResizeTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasResizeTests()
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

    private static ComponentInstance AddInstance(
        Board board,
        double x,
        double y,
        double width,
        double height
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

    private IRenderedComponent<DiagramCanvas> RenderSelected(
        Board board,
        ComponentInstance selected,
        bool snapToGrid = false
    )
    {
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, snapToGrid)
        );
        canvas.ClickOn(Container(canvas, selected.Id));
        return canvas;
    }

    private static IElement Container(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']");

    private static string ContainerStyle(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        Container(canvas, id).GetAttribute("style")!;

    private static IElement Handle(
        IRenderedComponent<DiagramCanvas> canvas,
        Guid id,
        string part
    ) => Container(canvas, id).QuerySelector($".resize-handle.{part}")!;

    [Fact]
    public void ResizingASelectedInstanceViaTheBottomRightHandleGrowsItByThePointerDistance()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var canvas = RenderSelected(board, instance);

        canvas.DragHandle(Handle(canvas, instance.Id, "bottom-right"), (150, 150), (190, 175));

        Assert.Equal(new Bounds(100, 100, 90, 75), instance.Bounds);
    }

    [Fact]
    public async Task ResizingScalesThePointerDistanceByTheCurrentZoom()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var canvas = RenderSelected(board, instance);
        await canvas.InvokeAsync(() => canvas.Instance.OnZoomIn());

        canvas.DragHandle(Handle(canvas, instance.Id, "bottom-right"), (300, 200), (344, 222));

        // Computed with the same arithmetic ZoomPanTracker uses (1.0 + 0.1), rather than the
        // decimal literal 1.1, so this can't disagree with production code over double rounding.
        var scale = 1.0 + 0.1;
        Assert.Equal(50 + 44 / scale, instance.Bounds.Width, precision: 10);
        Assert.Equal(50 + 22 / scale, instance.Bounds.Height, precision: 10);
        Assert.Equal(100, instance.Bounds.X, precision: 10);
        Assert.Equal(100, instance.Bounds.Y, precision: 10);
    }

    [Fact]
    public void TheShapeFollowsThePointerMidResizeAndTheBoardOnlyChangesOnRelease()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var canvas = RenderSelected(board, instance);

        canvas.PressHandle(Handle(canvas, instance.Id, "bottom-right"), (150, 150));
        canvas.MoveTo((200, 210));

        Assert.Contains("width: 100px; height: 110px", ContainerStyle(canvas, instance.Id));
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);

        canvas.ReleaseAt((200, 210));

        Assert.Equal(new Bounds(100, 100, 100, 110), instance.Bounds);
    }

    [Fact]
    public void ResizingViaTheTopLeftHandleKeepsTheOppositeCornerAnchoredOnTheBoard()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 100, 100);
        var canvas = RenderSelected(board, instance);

        canvas.DragHandle(Handle(canvas, instance.Id, "top-left"), (100, 100), (110, 120));

        Assert.Equal(new Bounds(110, 120, 90, 80), instance.Bounds);
    }

    [Fact]
    public void ResizingCannotShrinkBelowTheMinimumSizeOrInvertBounds()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 200, 200);
        var canvas = RenderSelected(board, instance);

        canvas.DragHandle(Handle(canvas, instance.Id, "bottom-right"), (300, 300), (-700, -700));

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public void UnderSnapOnlyTheMovingEdgeLandsOnAGridLineAndNothingJumpsAtRelease()
    {
        var board = new Board();
        var instance = AddInstance(board, 105, 103, 50, 50);
        var canvas = RenderSelected(board, instance, snapToGrid: true);

        canvas.PressHandle(Handle(canvas, instance.Id, "right"), (155, 128));
        canvas.MoveTo((193, 128));

        Assert.Contains(
            "left: 105px; top: 103px; width: 95px",
            ContainerStyle(canvas, instance.Id)
        );

        canvas.ReleaseAt((193, 128));

        Assert.Equal(new Bounds(105, 103, 95, 50), instance.Bounds);
    }

    [Fact]
    public void AReleaseOutsideTheCanvasCommitsTheResize()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var canvas = RenderSelected(board, instance);

        canvas.PressHandle(Handle(canvas, instance.Id, "left"), (100, 125));
        canvas.MoveTo((60, 125));
        canvas.ReleaseAt((-500, 125));

        Assert.Equal(new Bounds(60, 100, 90, 50), instance.Bounds);
    }

    [Fact]
    public async Task AStationaryClickOnAHandleLeavesNoHistoryEntryAndKeepsTheSelection()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var other = AddInstance(board, 300, 300, 50, 50);
        var canvas = RenderSelected(board, other);
        canvas.DragOn(Container(canvas, other.Id), (310, 310), (330, 310));
        canvas.ClickOn(Container(canvas, instance.Id));

        canvas.PressHandle(Handle(canvas, instance.Id, "bottom"), (125, 150));
        canvas.ReleaseAt((125, 150));

        Assert.Equal("true", Container(canvas, instance.Id).GetAttribute("aria-selected"));
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(300, 300, 50, 50), other.Bounds);
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task OneResizeIsOneUndoStep()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var canvas = RenderSelected(board, instance);

        canvas.PressHandle(Handle(canvas, instance.Id, "bottom-right"), (150, 150));
        canvas.MoveTo((170, 160));
        canvas.MoveTo((200, 190));
        canvas.ReleaseAt((200, 190));
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public void AnAttachedEdgeFollowsTheShapeThroughoutTheResize()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100, 50, 50);
        var target = AddInstance(board, 400, 100, 50, 50);
        board.AddEdge(
            new Edge(
                new PortEndpoint(source.Id, PortId.Right),
                new PortEndpoint(target.Id, PortId.Left)
            )
        );
        var canvas = RenderSelected(board, source);

        canvas.PressHandle(Handle(canvas, source.Id, "bottom-right"), (150, 150));
        canvas.MoveTo((190, 200));

        var line = canvas.Find(".edge-line");
        Assert.Equal("190", line.GetAttribute("x1"));
        Assert.Equal("150", line.GetAttribute("y1"));
    }
}
