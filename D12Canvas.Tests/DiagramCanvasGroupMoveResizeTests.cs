using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A persisted Group moves/resizes as one unit exactly like an ad-hoc multi-selection does -
// ExpandedSelection() already flattens a selected Group (nested or not) down to its raw leaf
// component instance ids before the existing move/resize machinery ever sees it, so there is no
// separate group-move/resize code path to add. These tests exist to pin that behaviour
// specifically for a *persisted* Group entity (including nesting), which the ad-hoc
// multi-selection tests never exercised together.
public class DiagramCanvasGroupMoveResizeTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasGroupMoveResizeTests()
    {
        SetupDiagramCanvasJsModule();
        JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");

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
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
    }

    [Fact]
    public async Task DraggingAMemberOfAPersistedGroupMovesEveryMemberByTheSameDelta()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        var containers = canvas.FindAll(".component-container");
        containers[0].MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        containers[0].MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        containers[0].MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 175 });

        Assert.Equal(new Bounds(140, 75, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(340, 75, 50, 50), second.Bounds);
    }

    [Fact]
    public async Task DraggingEmptySpaceWithinAPersistedGroupsBoundingBoxMovesTheWholeGroup()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        canvas
            .Find(".diagram-canvas")
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 25 });
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 200, ClientY = 75 });
        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 200, ClientY = 75 });

        Assert.Equal(new Bounds(50, 50, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(350, 50, 50, 50), second.Bounds);
    }

    [Fact]
    public async Task ResizingAPersistedGroupViaTheBottomRightHandleScalesEveryMemberProportionally()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        // Combined bbox starts at (0,0,200,50). Growing it to (0,0,300,100) scales x1.5/x2.
        var handle = canvas.Find(".group-resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 400, ClientY = 250 });
        handle.MouseUp(new MouseEventArgs { ClientX = 400, ClientY = 250 });

        Assert.Equal(new Bounds(0, 0, 75, 100), first.Bounds);
        Assert.Equal(new Bounds(150, 0, 150, 100), second.Bounds);
    }

    // Undo/redo-specific coverage (the "one history entry... undo restores all members
    // atomically" checklist item) lives in DiagramCanvasUndoRedoTests.cs instead, alongside
    // UndoAfterAGroupMove/ResizeRevertsEveryMemberInOneStep - same convention, now proven for a
    // persisted Group too.

    [Fact]
    public async Task ResizingAPersistedGroupViaTheTopLeftHandleKeepsTheOppositeCornerAnchored()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        // Combined bbox starts at (0,0,200,50), bottom-right corner (200,50). Dragging the
        // top-left handle outward (up-and-left) grows the bbox to (-20,-10,220,60) - the
        // opposite (bottom-right) corner must stay at exactly (200,50).
        var handle = canvas.Find(".group-resize-handle.top-left");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 280, ClientY = 190 });
        handle.MouseUp(new MouseEventArgs { ClientX = 280, ClientY = 190 });

        Assert.Equal(-20, first.Bounds.X, precision: 10);
        Assert.Equal(-10, first.Bounds.Y, precision: 10);
        Assert.Equal(200, second.Bounds.Right, precision: 10);
        Assert.Equal(50, second.Bounds.Bottom, precision: 10);
    }

    // A nested group ([A, B] grouped into an inner Group, then [inner, C] grouped into an outer
    // one) exercises the ExpandedSelection recursion - moving/resizing the outer group's
    // selection must move/scale every leaf (A, B, and C), not just the outer group's immediate
    // members.
    [Fact]
    public async Task MovingANestedGroupMovesEveryLeafMemberByTheSameDelta()
    {
        var board = new Board();
        var a = AddInstance(board, 0, 0);
        var b = AddInstance(board, 100, 0);
        var c = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas); // selects A and B
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed()); // inner group = {A, B}

        // Inner group is now the whole selection; shift-click C to add it, then group again to nest.
        canvas.FindAll(".component-container")[2].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed()); // outer group = {inner, C}
        Assert.Equal(2, board.Groups.Count);

        canvas
            .Find(".diagram-canvas")
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 25 });
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 200, ClientY = 75 });
        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 200, ClientY = 75 });

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas); // selects A and B
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed()); // inner group = {A, B}
        canvas.FindAll(".component-container")[2].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed()); // outer group = {inner, C}

        // Combined bbox is (0,0)-(400,50) = 400x50. Growing width to 800 doubles every x-extent.
        var handle = canvas.Find(".group-resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 400, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 800, ClientY = 200 });
        handle.MouseUp(new MouseEventArgs { ClientX = 800, ClientY = 200 });

        Assert.Equal(new Bounds(0, 0, 100, 50), a.Bounds);
        Assert.Equal(new Bounds(200, 0, 100, 50), b.Bounds);
        Assert.Equal(new Bounds(600, 0, 200, 50), c.Bounds);
    }
}
