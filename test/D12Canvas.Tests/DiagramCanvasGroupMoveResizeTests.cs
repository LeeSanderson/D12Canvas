using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasGroupMoveResizeTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasGroupMoveResizeTests()
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
        double width = 50,
        double height = 50
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

    private static void SelectBoth(IRenderedComponent<DiagramCanvas> canvas)
    {
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
    }

    [Fact]
    public async Task DraggingAMemberOfAPersistedGroupMovesEveryMemberByTheSameDelta()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        var containers = canvas.FindAll(".component-container");
        canvas.DragOn(containers[0], (300, 200), (340, 175));

        Assert.Equal(new Bounds(140, 75, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(340, 75, 50, 50), second.Bounds);
    }

    [Fact]
    public async Task DraggingEmptySpaceWithinAPersistedGroupsBoundingBoxMovesTheWholeGroup()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        canvas.PressOn(
            canvas.Find(".selection-bounding-box"),
            (150, 25),
            role: HitRole.SelectionBounds
        );
        canvas.MoveTo((200, 75));
        canvas.ReleaseAt((200, 75));

        Assert.Equal(new Bounds(50, 50, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(350, 50, 50, 50), second.Bounds);
    }

    [Fact]
    public async Task ResizingAPersistedGroupViaTheBottomRightHandleScalesEveryMemberProportionally()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        canvas.DragHandle(canvas.Find(".group-resize-handle.bottom-right"), (300, 200), (400, 250));

        Assert.Equal(new Bounds(0, 0, 75, 100), first.Bounds);
        Assert.Equal(new Bounds(150, 0, 150, 100), second.Bounds);
    }

    [Fact]
    public async Task ResizingAPersistedGroupViaTheTopLeftHandleKeepsTheOppositeCornerAnchored()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        canvas.DragHandle(canvas.Find(".group-resize-handle.top-left"), (300, 200), (280, 190));

        Assert.Equal(-20, first.Bounds.X, precision: 10);
        Assert.Equal(-10, first.Bounds.Y, precision: 10);
        Assert.Equal(200, second.Bounds.Right, precision: 10);
        Assert.Equal(50, second.Bounds.Bottom, precision: 10);
    }

    [Fact]
    public async Task MovingANestedGroupMovesEveryLeafMemberByTheSameDelta()
    {
        var board = new Board();
        var a = AddInstance(board, 0, 0);
        var b = AddInstance(board, 100, 0);
        var c = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        canvas.ClickOn(canvas.FindAll(".component-container")[2], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        Assert.Equal(2, board.Groups.Count);

        canvas.PressOn(
            canvas.Find(".selection-bounding-box"),
            (150, 25),
            role: HitRole.SelectionBounds
        );
        canvas.MoveTo((200, 75));
        canvas.ReleaseAt((200, 75));

        Assert.Equal(new Bounds(50, 50, 50, 50), a.Bounds);
        Assert.Equal(new Bounds(150, 50, 50, 50), b.Bounds);
        Assert.Equal(new Bounds(350, 50, 50, 50), c.Bounds);
    }

    [Fact]
    public async Task ResizingANestedGroupScalesEveryLeafMemberProportionally()
    {
        var board = new Board();
        var a = AddInstance(board, 0, 0, 50, 50);
        var b = AddInstance(board, 100, 0, 50, 50);
        var c = AddInstance(board, 300, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        canvas.ClickOn(canvas.FindAll(".component-container")[2], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        canvas.DragHandle(canvas.Find(".group-resize-handle.bottom-right"), (400, 200), (800, 200));

        Assert.Equal(new Bounds(0, 0, 100, 50), a.Bounds);
        Assert.Equal(new Bounds(200, 0, 100, 50), b.Bounds);
        Assert.Equal(new Bounds(600, 0, 200, 50), c.Bounds);
    }
}
